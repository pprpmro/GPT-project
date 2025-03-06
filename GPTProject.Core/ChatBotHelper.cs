using System.Text.Json;
using GPTProject.Core.Logger;
using GPTProject.Core.Providers.ChatGPT;
using GPTProject.Core.Providers.GigaChat;
using GPTProject.Core.Providers.YandexGPT;

namespace GPTProject.Core
{
	public class ChatBotHelper
	{
		private readonly ILogger logger;
		private readonly Dictionary<DialogType, IChatDialog> dialogs = new();
		private readonly Dictionary<string, (string SegmentPath, string MetadataPath)> availableTypesAndFileNames;
		private readonly string subjectArea;

		private Queue<string> pendingQuestions = new();
		private DialogState currentState;
		private List<int>? lastSourceTypeIndexes = null;
		private string? currentUserMessage;
		private string outputMessage = "";


		private int clarificationAttempts = 0;
		private const int MaxClarificationAttempts = 3;

		public DialogState DialogState { get { return currentState; } }

		public ChatBotHelper(
			Dictionary<DialogType, Type> providerTypes,
			string subjectArea,
			KnowledgeBaseFiles knowledgeBaseFiles,
			ILogger logger)
		{
			this.logger = logger;
			this.subjectArea = subjectArea;

			foreach (DialogType dialogType in Enum.GetValues(typeof(DialogType)))
			{
				if (!providerTypes.TryGetValue(dialogType, out var providerType))
				{
					throw new ArgumentException($"Поставщик для {dialogType} не задан в providerTypes.");
				}
				dialogs[dialogType] = GetChatDialogProvider(providerType);
			}
			availableTypesAndFileNames = GetAvailableTypesAndFileNames(knowledgeBaseFiles);

			InitializeSystemPrompts(knowledgeBaseFiles);
			this.currentState = DialogState.Waiting;
		}

		private IChatDialog GetChatDialogProvider(Type type) => type switch
		{
			Type.ChatGPT => new ChatGPTDialog(),
			Type.YandexGPT => new YandexGPTDialog(),
			Type.GigaChat => new GigaChatDialog(),
			_ => throw new NotImplementedException()
		};

		private void InitializeSystemPrompts(KnowledgeBaseFiles knowledgeBaseFiles)
		{
			if (dialogs.TryGetValue(DialogType.Cleansing, out var cleansingDialog))
			{
				cleansingDialog.UpdateSystemPrompt(PromptManager.GetCleansingPrompt());
			}

			if (dialogs.TryGetValue(DialogType.QuestionSeparator, out var questionSeparatorDialog))
			{
				questionSeparatorDialog.UpdateSystemPrompt(PromptManager.GetQuestionSeparatingPrompt());
			}

			if (dialogs.TryGetValue(DialogType.Classification, out var classificationDialog))
			{
				classificationDialog.UpdateSystemPrompt(
					PromptManager.GetClassificationPrompt(
						availableTypesAndFileNames.ToDictionary(
							kvp => kvp.Key,
							kvp => File.ReadAllText(kvp.Value.MetadataPath)
						)
					)
				);
			}
		}

		public string GetOutputMessage() => outputMessage.Trim();

		public void SetCurrentUserMessage(string message)
		{
			currentUserMessage = message;
		}

		public async Task<bool> Process()
		{
			try
			{
				StateLogging();

				return currentState switch
				{
					DialogState.Waiting => ProcessWaitingState(),
					DialogState.SmallTalk => await ProcessSmallTalkState(),
					DialogState.Separating => await ProcessSeparatingState(),
					DialogState.Replying => await ProcessReplyingState(),
					DialogState.Clarifying => await ProcessClarifyingState(),
					DialogState.Purging => await ProcessPurgingState(),
					_ => ProcessErrorState()
				};
			}
			catch (Exception ex)
			{
				logger.Log($"Exception in Process(): {ex.Message}\n{ex.StackTrace}", LogLevel.Error);
				currentState = DialogState.Error;
				return false;
			}
		}

		private bool ProcessWaitingState()
		{
			outputMessage = "";
			currentState = DialogState.SmallTalk;
			return true;
		}
		private bool ProcessErrorState()
		{
			outputMessage = "Can't get answer";
			logger.Log("Entered Error state", LogLevel.Error);
			currentState = DialogState.Waiting;
			return false;
		}
		private async Task<bool> ProcessSmallTalkState()
		{
			if (string.IsNullOrWhiteSpace(currentUserMessage))
			{
				logger.Log( "User message is empty", LogLevel.Error);
				return false;
			}

			var smallTalkResponse = await ProcessSmallTalk(currentUserMessage);

			if (smallTalkResponse.SmallTalk != "EMPTY")
			{
				outputMessage = smallTalkResponse.SmallTalk;
			}

			if (smallTalkResponse.Questions == "EMPTY")
			{
				currentState = DialogState.Waiting;
				return true;
			}

			currentUserMessage = smallTalkResponse.Questions;
			currentState = DialogState.Separating;
			return true;
		}
		private async Task<bool> ProcessSeparatingState()
		{
			if (string.IsNullOrWhiteSpace(currentUserMessage))
			{
				logger.Log("currentUserMessage is null", LogLevel.Error);
				return false;
			}

			pendingQuestions.Clear();
			var success = await SeparateQuestion(currentUserMessage);
			currentState = success ? DialogState.Replying : DialogState.Error;
			currentUserMessage = null;
			return success;
		}
		private async Task<bool> ProcessReplyingState()
		{
			if (pendingQuestions.Count == 0)
			{
				logger.Log("No questions to process", LogLevel.Error);
				return false;
			}

			bool needCleansing = pendingQuestions.Count > 1;
			outputMessage = "";

			while (pendingQuestions.Count > 0)
			{
				var questionToAnswer = pendingQuestions.Dequeue();
				var partialAnswer = await GetReply(questionToAnswer);

				if (partialAnswer.NeedСlarification)
				{
					outputMessage = partialAnswer.ClarificationQuestion;
					currentState = DialogState.Clarifying;
					return true;
				}

				outputMessage += partialAnswer.Response + Environment.NewLine;
			}

			currentState = needCleansing ? DialogState.Purging : DialogState.Waiting;
			return true;
		}
		private async Task<bool> ProcessClarifyingState()
		{
			if (currentUserMessage is null)
			{
				logger.Log("currentUserMessage is null", LogLevel.Error);
				return false;
			}

			if (clarificationAttempts >= MaxClarificationAttempts)
			{
				outputMessage = "Я не могу продолжать без более точной информации. Давайте попробуем другой вопрос.";
				currentState = DialogState.Waiting;
				return true;
			}

			var resultString = await dialogs[DialogType.User].SendMessage(currentUserMessage);
			var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

			if (result is null)
			{
				logger.Log("Can't parse answer", LogLevel.Error);
				return false;
			}

			if (result.NeedСlarification)
			{
				clarificationAttempts++;
				outputMessage = result.ClarificationQuestion + Environment.NewLine;
				return true;
			}

			outputMessage = result.Response + Environment.NewLine;
			currentState = DialogState.Replying;
			return true;
		}
		private async Task<bool> ProcessPurgingState()
		{
			outputMessage = await PurgingReply(outputMessage);
			currentState = DialogState.Waiting;
			return true;
		}


		private async Task<bool> SeparateQuestion(string userQuestion)
		{
			var separatedQuestionsString = await dialogs[DialogType.QuestionSeparator].SendMessage(userQuestion, rememberMessage: false);

			if (string.IsNullOrEmpty(separatedQuestionsString))
			{
				logger.Log("Пользователь не задал ни одного вопроса", LogLevel.Error);
				return false;
			}

			pendingQuestions = new Queue<string>(separatedQuestionsString.Split(';'));
			return true;
		}

		private async Task<HelperResponse> GetReply(string userPrompt)
		{
			var sourceIndexes = await GetRelevantSources(userPrompt);

			if (sourceIndexes.Contains(-1))
			{
				return new HelperResponse { NeedСlarification = false, Response = "Некорректный запрос" };
			}

			if (!lastSourceTypeIndexes?.SequenceEqual(sourceIndexes) ?? true)
			{
				UpdateSystemPrompt(sourceIndexes);
				lastSourceTypeIndexes = sourceIndexes;
			}

			return await FetchReplyFromBot(userPrompt);
		}

		private async Task<string> PurgingReply(string reply)
		{
			var cleansingAnswer = await dialogs[DialogType.Cleansing].SendMessage(reply, rememberMessage: false);

			if (string.IsNullOrEmpty(cleansingAnswer))
			{
				logger.Log("Не могу почистить", LogLevel.Error);
				return reply;
			}
			return cleansingAnswer;
		}

		private async Task<List<int>> GetRelevantSources(string userPrompt)
		{
			if (availableTypesAndFileNames.Count == 0)
			{
				logger.Log("Нет доступных типов", LogLevel.Error);
				return new List<int> { -1 };
			}

			var typesString = await dialogs[DialogType.Classification].SendMessage(userPrompt);
			ClassificationLogging(typesString);

			var selectedIndexes = typesString.Split(';')
											 .Select(t => int.TryParse(t, out var result) ? result : -1)
											 .Where(i => i >= 1 && i <= availableTypesAndFileNames.Count)
											 .Select(i => i - 1)
											 .ToList();

			return selectedIndexes.Count > 0 ? selectedIndexes : new List<int> { -1 };
		}

		private void UpdateSystemPrompt(List<int> sourceIndexes)
		{
			var selectedSegments = sourceIndexes
				.Select(i => availableTypesAndFileNames.ElementAtOrDefault(i))
				.Where(kvp => kvp.Key != null)
				.Select(kvp => kvp.Value.SegmentPath)
				.Select(GetSourceByPath)
				.Where(source => !string.IsNullOrEmpty(source))
				.ToList();

			if (selectedSegments.Count == 0)
			{
				logger.Log("Нет подходящих сегментов", LogLevel.Error);
				return;
			}

			dialogs[DialogType.User].UpdateSystemPrompt(PromptManager.GetSystemPrompt(subjectArea, selectedSegments), false);
		}

		private string GetSourceByPath(string path) => File.ReadAllText(path);

		private async Task<HelperResponse> FetchReplyFromBot(string userPrompt)
		{
			var resultString = await dialogs[DialogType.User].SendMessage(userPrompt);
			var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

			if (result is null)
			{
				logger.Log("Failed to parse bot response", LogLevel.Error);
				return new HelperResponse { NeedСlarification = false, Response = "Ошибка обработки ответа" };
			}

			return result;
		}

		private async Task<SmallTalkResponse> ProcessSmallTalk(string userMessage)
		{
			var gptResponse = await dialogs[DialogType.SmallTalk].SendMessage(userMessage);

			try
			{
				return JsonSerializer.Deserialize<SmallTalkResponse>(gptResponse) ??
						new SmallTalkResponse { SmallTalk = "EMPTY", Questions = "EMPTY" };
			}
			catch (Exception ex)
			{
				logger.Log($"Ошибка при обработке Small Talk JSON: {ex.Message}", LogLevel.Error);
				return new SmallTalkResponse { SmallTalk = "EMPTY", Questions = "EMPTY" };
			}
		}

		private Dictionary<string, (string SegmentPath, string MetadataPath)> GetAvailableTypesAndFileNames(KnowledgeBaseFiles knowledgeBaseFiles)
		{
			var availableFiles = new Dictionary<string, (string, string)>();

			foreach (var segmentPath in knowledgeBaseFiles.SegmentPaths)
			{
				string fileName = Path.GetFileNameWithoutExtension(segmentPath);
				string? metadataPath = knowledgeBaseFiles.MetadataPaths
					.FirstOrDefault(meta => Path.GetFileNameWithoutExtension(meta) == $"{fileName}.meta");

				if (metadataPath == null)
				{
					logger.Log($"Метаданные для {fileName} не найдены!", LogLevel.Warning);
					continue;
				}

				availableFiles[fileName] = (segmentPath, metadataPath);
			}

			if (availableFiles.Count == 0)
			{
				throw new Exception("Файлы не найдены!");
			}

			return availableFiles;
		}

		private void StateLogging()
		{
			logger.Log($"Current state: {currentState}", LogLevel.Info);
		}

		private void ClassificationLogging(string types)
		{
			logger.Log("Classes of question: " + types, LogLevel.Info);
		}
	}

	public enum DialogState
	{
		Waiting,
		SmallTalk,
		Separating,
		Clarifying,
		Replying,
		Purging,
		Error
	}

	/*
	graph TD;
		A[Waiting] -->|Проверка Small Talk| B[SmallTalk]
		B -->|Small Talk найден| C[Ответ пользователю и возврат в Waiting]
		B -->|Small Talk отсутствует| D[Separating]
		D -->|Разделение на вопросы| E[Classification]
		E -->|Категория найдена| F[Replying]
		E -->|Категория не найдена (-1)| G[Ответ "Не по теме" и возврат в Waiting]
		F -->|Ответ найден| H[Purging]
		F -->|Недостаточно информации| I[Clarifying]
		I -->|Уточнение получено| F
		I -->|3 неудачных попытки| J[Ответ "Я не могу продолжать" и возврат в Waiting]
		H -->|Ответ обработан| A
		F -->|Ошибка| K[Error]
		K -->|Лог ошибки и возврат в Waiting| A
	*/
}