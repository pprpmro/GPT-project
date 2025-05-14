using GPTProject.Core.Models;
using GPTProject.Common.Logging;
using GPTProject.Common.Utils;
using System.Text.RegularExpressions;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Common;
using GPTProject.Providers.Dialogs.Enumerations;

namespace GPTProject.Core.ChatBot
{
	public class Agent
	{
		private readonly ILogger logger;
		private readonly Dictionary<DialogType, IChatDialog> dialogs = new Dictionary<DialogType, IChatDialog>();
		private readonly Dictionary<string, (string SegmentPath, string MetadataPath)> availableTypesAndFileNames;
		private readonly string subjectArea;

		private Queue<string> pendingQuestions = new();
		private DialogState currentState;
		private List<int>? lastSourceTypeIndexes = null;
		private string? currentUserMessage;
		private string outputMessage = "";
		private string clarifyResponse = "";

		private string outputQuestionMessage = "";


		private int clarificationAttempts = 0;
		private const int MaxClarificationAttempts = 3;

		public int TotalSendedTokenCount
		{
			get
			{
				return dialogs.Values.Sum(dialog => dialog.SessionTokenUsage);
			}
		}

		public DialogState DialogState { get { return currentState; } }

		public Agent(
			Dictionary<DialogType, ProviderType> providerTypes,
			string subjectArea,
			KnowledgeBaseFiles knowledgeBaseFiles,
			ILogger logger)
		{
			this.logger = logger;
			this.subjectArea = subjectArea;

			var requiredDialogTypes = new List<DialogType> { DialogType.User, DialogType.Classification, DialogType.Cleansing, DialogType.QuestionSeparator, DialogType.SmallTalk };

			foreach (DialogType dialogType in requiredDialogTypes)
			{ 
				dialogs[dialogType] = DialogSelector.GetDialog(providerTypes, dialogType);
			}
			availableTypesAndFileNames = GetAvailableTypesAndFileNames(knowledgeBaseFiles);

			InitializeSystemPrompts();
			currentState = DialogState.Waiting;
		}

		private void InitializeSystemPrompts()
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

			if (dialogs.TryGetValue(DialogType.SmallTalk, out var smallTalkDialog))
			{
				smallTalkDialog.UpdateSystemPrompt(PromptManager.GetSmallTalkPrompt(subjectArea));
			}
		}

		private string GetOutputMessage() => outputMessage.Trim();
		private string GetOutputQuestionMessage() => outputQuestionMessage.Trim();

		private void SetCurrentUserMessage(string message)
		{
			currentUserMessage = message;
		}

		private void SetWaitingState()
		{
			currentState = DialogState.Waiting;
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction)
		{
			while (true)
			{
				if (DialogState is DialogState.Waiting or DialogState.Clarifying)
				{
					var userMessage = await GetUserMessageFunction();
					if (userMessage.Equals("exit", StringComparison.OrdinalIgnoreCase))
					{
						logger.Log("Завершение работы чат-бота...", LogLevel.Info);
						break;
					}
					SetCurrentUserMessage(userMessage);
				}


				bool success = await Process();
				if (!success)
				{
					logger.Log("Ошибка обработки запроса. Переключение на состояние Waiting", LogLevel.Error);
					SetWaitingState();
					continue;
				}

				if (DialogState is DialogState.Clarifying)
				{
					Console.WriteLine(GetOutputQuestionMessage());
				}


				if (DialogState is DialogState.Waiting or DialogState.Clarifying or DialogState.Error)
				{
					Console.WriteLine(GetOutputMessage());
				}
			}
			logger.Log($"Потрачено на диалог: {TotalSendedTokenCount}", LogLevel.Error);
		}

		private async Task<bool> Process()
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

		//TODO Продумать как можно сделать чтобы обработать случаи, када оба поля не пусты
		private async Task<bool> ProcessSmallTalkState()
		{
			if (string.IsNullOrWhiteSpace(currentUserMessage))
			{
				logger.Log("User message is empty", LogLevel.Error);
				return false;
			}

			var smallTalkResponse = await ProcessSmallTalk(currentUserMessage);
			bool isQuestionsEmpty = string.IsNullOrEmpty(smallTalkResponse.Questions) || smallTalkResponse.Questions.Equals("EMPTY", StringComparison.OrdinalIgnoreCase);
			bool isSmallTalkEmpty = string.IsNullOrEmpty(smallTalkResponse.SmallTalk) || smallTalkResponse.SmallTalk.Equals("EMPTY", StringComparison.OrdinalIgnoreCase);

			if (isSmallTalkEmpty && isQuestionsEmpty)
			{
				outputMessage = Helper.GetRandomUnclearResponse();
				currentState = DialogState.Waiting;
				return true;
			}

			if (!isQuestionsEmpty)
			{
				currentUserMessage = smallTalkResponse.Questions;
				currentState = DialogState.Separating;
				return true;
			}

			if (!isSmallTalkEmpty)
			{
				outputMessage = smallTalkResponse.SmallTalk;
				currentState = DialogState.Waiting;
				return true;
			}

			return false;
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
			if (pendingQuestions.Count == 0 && string.IsNullOrEmpty(clarifyResponse))
			{
				logger.Log("No questions to process", LogLevel.Error);
				return false;
			}

			bool needCleansing = pendingQuestions.Count > 1 ||
						(pendingQuestions.Count == 1 && !string.IsNullOrEmpty(clarifyResponse));

			if (!string.IsNullOrEmpty(clarifyResponse))
			{
				outputMessage += clarifyResponse + Environment.NewLine;
				clarifyResponse = "";
			}

			while (pendingQuestions.Count > 0)
			{
				var questionToAnswer = pendingQuestions.Dequeue();
				var partialAnswer = await GetReply(questionToAnswer);

				if (!string.IsNullOrEmpty(partialAnswer.ClarificationQuestion))
				{
					outputQuestionMessage = partialAnswer.ClarificationQuestion;
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

			if (clarificationAttempts > MaxClarificationAttempts)
			{
				outputMessage = "Я не могу продолжать без более точной информации. Давайте попробуем другой вопрос.";
				currentState = DialogState.Waiting;
				return true;
			}

			clarifyResponse = "";
			var resultString = await dialogs[DialogType.User].SendMessage(currentUserMessage);
			var result = await FetchReplyFromBot(resultString);

			if (result is null)
			{
				logger.Log("Can't parse answer", LogLevel.Error);
				return false;
			}

			if (!string.IsNullOrEmpty(result.ClarificationQuestion))
			{
				clarificationAttempts++;
				outputQuestionMessage = result.ClarificationQuestion;
				return true;
			}

			clarifyResponse = result.Response + Environment.NewLine;
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
			SeparatedQuestionsLogging(separatedQuestionsString);

			if (string.IsNullOrEmpty(separatedQuestionsString))
			{
				logger.Log("Пользователь не задал ни одного вопроса", LogLevel.Error);
				return false;
			}

			pendingQuestions = new Queue<string>(separatedQuestionsString.Split(';'));
			return true;
		}
		private async Task<UserChatResponse> GetReply(string userPrompt)
		{
			var sourceIndexes = await GetRelevantSources(userPrompt);

			if (sourceIndexes.Contains(-1))
			{
				return new UserChatResponse { Response = "В моей базе знаний нет информации как отвечать на этот запрос" };
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
		private async Task<UserChatResponse> FetchReplyFromBot(string userPrompt)
		{
			var resultString = await dialogs[DialogType.User].SendMessage(userPrompt);
			var responseMatch = Regex.Match(resultString, @"RESPONSE:\s*(.*)");
			var clarificationMatch = Regex.Match(resultString, @"CLARIFICATION_QUESTION:\s*(.*)");

			var response = responseMatch.Success ? responseMatch.Groups[1].Value.Trim() : "";
			var clarificationQuestion = clarificationMatch.Success ? clarificationMatch.Groups[1].Value.Trim() : "";

			var result = new UserChatResponse
			{
				Response = string.IsNullOrEmpty(response) || response.Equals("EMPTY", StringComparison.OrdinalIgnoreCase)
				? ""
				: response,

				ClarificationQuestion = string.IsNullOrEmpty(clarificationQuestion) || clarificationQuestion.Equals("EMPTY", StringComparison.OrdinalIgnoreCase)
				? ""
		:		 clarificationQuestion
			};

			if (result is null)
			{
				logger.Log("Failed to parse bot response", LogLevel.Error);
				return new UserChatResponse { Response = "Ошибка обработки ответа" };
			}

			if (!string.IsNullOrEmpty(result.ClarificationQuestion))
			{
				ClarificationQuestionsLogging();
			}
			return result;
		}
		private async Task<SmallTalkResponse> ProcessSmallTalk(string userMessage)
		{
			var gptResponse = await dialogs[DialogType.SmallTalk].SendMessage(userMessage, null, false);

			try
			{
				var questionsMatch = Regex.Match(gptResponse, @"QUESTIONS:\s*(.*)");
				var smallTalkMatch = Regex.Match(gptResponse, @"SMALL_TALK:\s*(.*)");

				var questions = questionsMatch.Success ? questionsMatch.Groups[1].Value.Trim() : "EMPTY";
				var smallTalk = smallTalkMatch.Success ? smallTalkMatch.Groups[1].Value.Trim() : "EMPTY";

				return new SmallTalkResponse
				{
					Questions = string.IsNullOrEmpty(questions) ? "EMPTY" : questions,
					SmallTalk = string.IsNullOrEmpty(smallTalk) ? "EMPTY" : smallTalk
				};
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
			if (string.IsNullOrWhiteSpace(types) || types =="-1")
			{
				logger.Log("Classes of question: NONE", LogLevel.Info);
				return;
			}

			var typeIndexes = types.Split(';', StringSplitOptions.RemoveEmptyEntries);
			var classifiedTypes = typeIndexes
				.Select(type => availableTypesAndFileNames.Keys.ElementAtOrDefault(int.Parse(type.Trim())-1))
				.Where(typeName => typeName != null)
				.ToList();

			string logMessage = classifiedTypes.Any()
				? $"Classes of question: {string.Join(", ", classifiedTypes)}"
				: "Classes of question: UNKNOWN";

			logger.Log(logMessage, LogLevel.Info);
		}
		private void SeparatedQuestionsLogging(string questions)
		{
			logger.Log("Separated questions: " + questions, LogLevel.Info);
		}
		private void ClarificationQuestionsLogging()
		{
			logger.Log("Clarifying Is Needed", LogLevel.Warning);
		}
	}
}