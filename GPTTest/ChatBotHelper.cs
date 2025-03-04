using System.Text.Json;
using System.Text.Json.Serialization;
using GPTProject.Core.Logger;
using GPTProject.Core.Providers.ChatGPT;
using GPTProject.Core.Providers.GigaChat;
using GPTProject.Core.Providers.YandexGPT;

namespace GPTProject.Core
{
	public class ChatBotHelper
	{
		private readonly IChatDialog userDialog;
		private readonly IChatDialog classificationDialog;

		private readonly IChatDialog cleansingDialog;
		private readonly IChatDialog questionSeparatorDialog;

		private readonly ILogger logger;
		private readonly Dictionary<string, string> availableTypesAndFileNames;
		private readonly string subjectArea;

		private string outputMessage = "";


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

			this.cleansingDialog.SetSystemPrompt(message: PromptManager.GetCleansingPrompt());
			this.questionSeparatorDialog.SetSystemPrompt(message: PromptManager.GetQuestionSeparatingPrompt());

			this.availableTypesAndFileNames = GetAvailableTypesAndFileNames(filePaths);
			this.classificationDialog.SetSystemPrompt(message: PromptManager.GetClassificationPrompt(AvailableTypes));
			this.currentState = DialogState.Waiting;
		}

		private DialogState currentState;


		private Queue<string>? separatedQuestions = null;
		

		private Queue<string> questionsToAnswer = new Queue<string>();

		private List<int>? lastSourceTypeIndexes = null;

		private string? currentUserMessage;

		public void SetCurrentUserMessage(string message)
		{
			currentUserMessage = message;
		}

		public string GetOutputMessage() => outputMessage ?? "";

		public async Task<bool> Process()
		{
			switch (currentState)
			{
				case DialogState.Waiting:
				{
					outputMessage = "";
					StateLogging();
					currentState = DialogState.Separating;
					return true;
				}
				case DialogState.Separating:
				{
					StateLogging();
					if (string.IsNullOrWhiteSpace(currentUserMessage))
					{
						throw new ArgumentException("currentUserMessage is null");
					}
					var success = await SeparateQuestion(currentUserMessage);
					if (success)
					{
						currentState = DialogState.Replying;
					}
					else
					{
						currentState = DialogState.Error;
					}
					currentUserMessage = null;
					return true;
				}
				case DialogState.Replying:
				{
					StateLogging();

					if (separatedQuestions is null)
					{
						throw new Exception("separatedQuestions is null");
					}

					var needCleansing = questionsToAnswer.Count > 1;

					while (separatedQuestions.Count > 0)
					{
						var questionToAnswer = separatedQuestions.Dequeue();
						var partialAnswer = await GetReply(questionToAnswer);

						if (partialAnswer.NeedСlarification)
						{
							outputMessage = partialAnswer.ClarificationQuestion;
							currentState = DialogState.Clarifying;
							return true;
						}
						else
						{
							outputMessage += partialAnswer.Response + Environment.NewLine;
						}
					}

					if (needCleansing)
					{
						currentState = DialogState.Purging;
					}
					else
					{
						currentState = DialogState.Waiting;
					}

					return true;
				}
				case DialogState.Clarifying:
				{
					StateLogging();
					if (currentUserMessage is null)
					{
						throw new ArgumentNullException("currentUserMessage is null");
					}

					var resultString = await userDialog.SendMessage(currentUserMessage);
					var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

					if (result is null)
					{
						throw new Exception("Can't parse answer");
					}

					if (result.NeedСlarification)
					{
						outputMessage = result.ClarificationQuestion;
					}
					else
					{
						outputMessage = result.Response + Environment.NewLine;
						currentState = DialogState.Replying;
					}

					return true;
				}
				case DialogState.Purging:
				{
					StateLogging();
					currentState = DialogState.Waiting;
					outputMessage = await PurgingReply(outputMessage);
					return true;
				}

				default:
					throw new Exception("Incorrect State");
			}
			throw new Exception("Incorrect ending");
		}

		private async Task<bool> SeparateQuestion(string userQuestion)
		{
			var separatedQuestionsString = await questionSeparatorDialog.SendMessage(userQuestion);

			if (string.IsNullOrEmpty(separatedQuestionsString))
			{
				throw new Exception("Пользователь не задал ни одного вопроса");
			}
			separatedQuestions = new Queue<string>( separatedQuestionsString.Split(new char[] { ';' }));//Небезопасно если в ходе текста встретится такой символ
			questionSeparatorDialog.ClearDialog(false);
			return true;
		}
		private async Task<string> PurgingReply(string reply)
		{
			var cleanisingAnswer = await cleansingDialog.SendMessage(reply);

			if (string.IsNullOrEmpty(cleanisingAnswer))
			{
				throw new Exception("Не могу почистить");
			}
			cleansingDialog.ClearDialog(false);
			return cleanisingAnswer;
		}
		private async Task<HelperResponse> GetReply(string userPrompt)
		{
			var sourceTypeIndexes = await GetSourcesTypeByUserPrompt(userPrompt);
			if (sourceTypeIndexes.Count == 0 || sourceTypeIndexes.Contains(-1))//TODO сделать более грамотное отсеивание некорректный запросов
			{
				return new HelperResponse
				{
					NeedСlarification = false,
					Response = "Некорректный запрос"
				};
			}

			var isEqual = lastSourceTypeIndexes?.SequenceEqual<int>(sourceTypeIndexes) ?? false;

			if (!isEqual)
			{
				if (sourceTypeIndexes.Contains(AvailableTypes.Count))
				{
					userDialog.ReplaceSystemPrompt(message: PromptManager.GetSystemPrompt(subjectArea, null), clearDialog: false);
				}
				else
				{
					var sources = new List<string>();
					foreach (var index in sourceTypeIndexes)
					{
						var sourceType = AvailableTypes[index];
						var path = availableTypesAndFileNames[sourceType];

						var source = GetSourceByPath(path);
						if (string.IsNullOrEmpty(source))
						{
							throw new Exception("Source was null");
						}
						sources.Add(source);
					}
					userDialog.ReplaceSystemPrompt(message: PromptManager.GetSystemPrompt(subjectArea, sources), clearDialog: false);
				}
				lastSourceTypeIndexes = sourceTypeIndexes;
			}

			var resultString = await userDialog.SendMessage(userPrompt);
			var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

			if (result is not null)
			{
				return result;
			}
			throw new Exception("Can't parse answer");
		}
		private async Task<List<int>> GetSourcesTypeByUserPrompt(string userPrompt)
		{
			if (AvailableTypes.Count == 0)
			{
				throw new Exception("Types dont exist");
			}
			var typesString = await classificationDialog.SendMessage(userPrompt);
			ClassificationLogging(typesString);
			var types = typesString.Split(new char[] { ';' });

			var listOfTypes = new List<int>();

			for (int i = 0; i < types.Length; i++)
			{
				var parseResult = int.TryParse(types[i], out var resultSource);
				if (parseResult)
				{
					listOfTypes.Add(resultSource);
				}
			}
			return listOfTypes;
		}
		private string GetSourceByPath(string path)
		{
			var result = string.Empty;
			using (var reader = new StreamReader(path))
			{
				result = reader.ReadToEnd();
			}
			return result;
		}

		private Dictionary<string,string> GetAvailableTypesAndFileNames(List<string> filePaths)
		{
			var availableTypesAndFileNames = new Dictionary<string,string>();
			foreach (var filePath in filePaths)
			{
				if (File.Exists(filePath))
				{
					string fileName = Path.GetFileName(filePath);
					availableTypesAndFileNames[fileName] = filePath;
				}
				else
				{
					Console.WriteLine($"Can't find: {filePath}");
				}
			}

			if (availableTypesAndFileNames.Count == 0)
			{
				throw new Exception("Files don't exist");
			}
			return availableTypesAndFileNames;
		}
		private IChatDialog GetChatDialogProvider(Type type)
		{
			switch (type)
			{
				case Type.ChatGPT:
					return new ChatGPTDialog();
				case Type.YandexGPT:
					return new YandexGPTDialog();
				case Type.GigaChat:
					return new GigaChatDialog();
				default:
					throw new NotImplementedException();
			}
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

	public class HelperResponse
	{
		[JsonPropertyName("clarificationQuestion")]
		public string? ClarificationQuestion { get; set; }
		[JsonPropertyName("response")]
		public string? Response { get; set; }
		[JsonPropertyName("needСlarification")]
		public bool NeedСlarification { get; set; }
	}


	public enum DialogState
	{
		Waiting,
		Separating,
		Clarifying,
		Replying,
		Purging,
		Error
	}
}