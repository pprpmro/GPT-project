using System.Text.Json;
using System.Text.RegularExpressions;
using GPTProject.Core.Logger;
using GPTProject.Core.Providers.ChatGPT;
using GPTProject.Core.Providers.GigaChat;
using GPTProject.Core.Providers.YandexGPT;

namespace GPTProject.Core
{
	public class ChatBotHelper
	{
		private readonly ILogger logger;
		private readonly IChatDialog userDialog;
		private readonly IChatDialog classificationDialog;
		private readonly IChatDialog cleansingDialog;
		private readonly IChatDialog questionSeparatorDialog;
		private readonly IChatDialog smallTalkDialog;
		private readonly Dictionary<string, string> availableTypesAndFileNames;
		private readonly string subjectArea;

		private Queue<string> pendingQuestions = new();

		private DialogState currentState;
		private List<int>? lastSourceTypeIndexes = null;
		private string? currentUserMessage;
		private string outputMessage = "";

		private int clarificationAttempts = 0;
		private const int MaxClarificationAttempts = 3;

		private List<string> AvailableTypes
		{
			get
			{
				if (availableTypesAndFileNames is null || availableTypesAndFileNames.Count == 0)
				{
					return new List<string>();
				}
				return availableTypesAndFileNames.Select(x => x.Key).ToList();
			}
		}

		public DialogState DialogState { get { return currentState; } }

		public ChatBotHelper(
			Type providerType,
			string subjectArea,
			List<string> filePaths,
			ILogger logger)
		{
			this.logger = logger;
			this.subjectArea = subjectArea;
			this.classificationDialog = GetChatDialogProvider(providerType);
			this.userDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog = GetChatDialogProvider(providerType);
			this.questionSeparatorDialog = GetChatDialogProvider(providerType);

			this.smallTalkDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog.SetSystemPrompt(message: PromptManager.GetCleansingPrompt());
			this.questionSeparatorDialog.SetSystemPrompt(message: PromptManager.GetQuestionSeparatingPrompt());

			this.availableTypesAndFileNames = GetAvailableTypesAndFileNames(filePaths);
			this.classificationDialog.SetSystemPrompt(message: PromptManager.GetClassificationPrompt(AvailableTypes));

			this.smallTalkDialog.SetSystemPrompt(message: PromptManager.GetSmallTalkPrompt());

			this.currentState = DialogState.Waiting;
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
			currentState = DialogState.Separating;
			return true;
		}
		private bool ProcessErrorState()
		{
			logger.Log("Entered Error state", LogLevel.Error);
			return false;
		}
		private async Task<bool> ProcessSmallTalkState()
		{
			if (string.IsNullOrWhiteSpace(currentUserMessage))
			{
				logger.Log( "User message is empty", LogLevel.Error);
				return false;
			}

			var (smallTalkReply, filteredQuestions) = await ProcessSmallTalk(currentUserMessage);

			if (smallTalkReply != "EMPTY")
			{
				outputMessage = smallTalkReply;
			}

			if (filteredQuestions == "EMPTY")
			{
				currentState = DialogState.Waiting;
				return true;
			}

			currentUserMessage = filteredQuestions;
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

			var resultString = await userDialog.SendMessage(currentUserMessage);
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
			var separatedQuestionsString = await questionSeparatorDialog.SendMessage(userQuestion);

			if (string.IsNullOrEmpty(separatedQuestionsString))
			{
				logger.Log("Пользователь не задал ни одного вопроса", LogLevel.Error);
				return false;
			}

			pendingQuestions = new Queue<string>(separatedQuestionsString.Split(';'));
			questionSeparatorDialog.ClearDialog(false);
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
			var cleansingAnswer = await cleansingDialog.SendMessage(reply);

			if (string.IsNullOrEmpty(cleansingAnswer))
			{
				logger.Log("Не могу почистить", LogLevel.Error);
				return reply;
			}

			cleansingDialog.ClearDialog(false);
			return cleansingAnswer;
		}

		private async Task<List<int>> GetRelevantSources(string userPrompt)
		{
			if (AvailableTypes.Count == 0)
			{
				logger.Log("No available types", LogLevel.Error);
				return new List<int> { -1 };
			}

			var typesString = await classificationDialog.SendMessage(userPrompt);
			ClassificationLogging(typesString);

			return typesString.Split(';')
							  .Select(t => int.TryParse(t, out var result) ? result : -1)
							  .ToList();
		}

		private void UpdateSystemPrompt(List<int> sourceIndexes)
		{
			if (sourceIndexes.Contains(AvailableTypes.Count))
			{
				userDialog.ReplaceSystemPrompt(PromptManager.GetSystemPrompt(subjectArea, null), false);
				return;
			}

			var sources = sourceIndexes.Select(i => AvailableTypes[i])
										.Select(type => availableTypesAndFileNames[type])
										.Select(GetSourceByPath)
										.Where(source => !string.IsNullOrEmpty(source))
										.ToList();

			if (sources.Count == 0)
			{
				logger.Log("No valid sources found", LogLevel.Error);
				return;
			}

			userDialog.ReplaceSystemPrompt(PromptManager.GetSystemPrompt(subjectArea, sources), false);
		}

		private string GetSourceByPath(string path) => File.ReadAllText(path);

		private async Task<HelperResponse> FetchReplyFromBot(string userPrompt)
		{
			var resultString = await userDialog.SendMessage(userPrompt);
			var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

			if (result is null)
			{
				logger.Log("Failed to parse bot response", LogLevel.Error);
				return new HelperResponse { NeedСlarification = false, Response = "Ошибка обработки ответа" };
			}

			return result;
		}

		private async Task<(string smallTalkReply, string filteredQuestions)> ProcessSmallTalk(string userMessage)
		{
			var gptResponse = await smallTalkDialog.SendMessage(userMessage);
			var smallTalkMatch = Regex.Match(gptResponse, @"SMALL_TALK:\s*(.*)");
			var questionsMatch = Regex.Match(gptResponse, @"QUESTIONS:\s*(.*)");

			string smallTalkReply = smallTalkMatch.Success ? smallTalkMatch.Groups[1].Value.Trim() : "EMPTY";
			string filteredQuestions = questionsMatch.Success ? questionsMatch.Groups[1].Value.Trim() : "EMPTY";

			return (smallTalkReply, filteredQuestions);
		}

		private Dictionary<string, string> GetAvailableTypesAndFileNames(List<string> filePaths)
		{
			var availableTypesAndFileNames = new Dictionary<string, string>();
			foreach (var filePath in filePaths)
			{
				if (File.Exists(filePath))
				{
					availableTypesAndFileNames[Path.GetFileName(filePath)] = filePath;
				}
				else
				{
					logger.Log($"Can't find file: {filePath}", LogLevel.Warning);
				}
			}

			if (availableTypesAndFileNames.Count == 0)
			{
				throw new Exception("Files don't exist");
			}
			return availableTypesAndFileNames;
		}

		private IChatDialog GetChatDialogProvider(Type type) => type switch
		{
			Type.ChatGPT => new ChatGPTDialog(),
			Type.YandexGPT => new YandexGPTDialog(),
			Type.GigaChat => new GigaChatDialog(),
			_ => throw new NotImplementedException()
		};

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
	A[Waiting] -->|Получили сообщение| B[Separating];
	B -->|Разбили на вопросы| C[Replying];
	C -->|Определили тему вопроса| D[UpdateSystemPrompt];
	D -->|Получили ответ| E[Clarifying];
	D -->|Ответ готов| F[Purging];
	E -->|Запросили уточнение| C;
	F -->|Очистили ответ| A;
	C -->|Ошибка| G[Error];
	G -->|Логируем| A;
*/
}