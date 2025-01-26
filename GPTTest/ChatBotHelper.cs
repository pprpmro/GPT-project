using System.Text.Json;
using System.Text.Json.Serialization;
using GPTProject.Common;
using GPTProject.Core;
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
		private readonly IChatDialog clarifyingDialog;
		private readonly IChatDialog questionSeparatorDialog;

		private readonly Dictionary<string, string> availableTypesAndFileNames;
		private readonly string subjectArea;

		private const bool loggingEnabled = true;

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
			List<string> filePaths)
		{
			this.subjectArea = subjectArea;
			this.classificationDialog = GetChatDialogProvider(providerType);
			this.userDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog = GetChatDialogProvider(providerType);
			this.clarifyingDialog = GetChatDialogProvider(providerType);
			this.questionSeparatorDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog.SetSystemPrompt(message: GetСleansingPrompt());
			this.questionSeparatorDialog.SetSystemPrompt(message: GetQuestionSeparatingPrompt());
			this.clarifyingDialog.SetSystemPrompt(message: GetClarifyingPrompt());

			this.availableTypesAndFileNames = GetAvailableTypesAndFileNames(filePaths);
			this.classificationDialog.SetSystemPrompt(message: GetClassificationPrompt(AvailableTypes));
			this.currentState = DialogState.Awaiting;
		}

		private DialogState currentState;


		private Queue<string>? separatedQuestions = null;
		

		private Queue<string> questionsToAnswer = new Queue<string>();

		private List<int>? lastSourceTypeIndexes = null;

		private string? currentUserMessage;
		private string? questionToClarify = null;

		public void SetCurrentUserMessage(string message)
		{
			currentUserMessage = message;
		}

		public string GetOutputMessage() => outputMessage ?? "";

		public async Task<bool> Process()
		{
			switch (currentState)
			{
				case DialogState.Awaiting:
				{
					outputMessage = "";
					StateLogging(loggingEnabled);
					currentState = DialogState.Separating;
					return true;
				}
				case DialogState.Separating:
				{
					StateLogging(loggingEnabled);
					if (string.IsNullOrWhiteSpace(currentUserMessage))
					{
						throw new ArgumentException("currentUserMessage is null");
					}
					var success = await SeparateQuestion(currentUserMessage);
					if (success)
					{
						currentState = DialogState.Answering;
					}
					else
					{
						currentState = DialogState.Error;
					}
					currentUserMessage = null;
					return true;
				}
				case DialogState.Answering:
				{
					StateLogging(loggingEnabled);

					if (separatedQuestions is null)
					{
						throw new Exception("separatedQuestions is null");
					}

					var needCleansing = questionsToAnswer.Count > 1;

					while (separatedQuestions.Count > 0)
					{
						var questionToAnswer = separatedQuestions.Dequeue();
						var partialAnswer = await GetAnswer(questionToAnswer);

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
						currentState = DialogState.Cleansing;
					}
					else
					{
						currentState = DialogState.Awaiting;
					}

					return true;
				}
				case DialogState.Clarifying:
				{
					if (questionToClarify is null)
					{
						throw new ArgumentNullException("questionToClarify is null");
					}

					var resultString = await userDialog.SendMessage(questionToClarify);
					var result = JsonSerializer.Deserialize<HelperResponse>(resultString);

					if (result is null)
					{
						throw new Exception("Can't parse answer");
					}

					if (result.NeedСlarification)
					{
						questionToClarify = result.ClarificationQuestion;
					}
					else
					{
						outputMessage += result.Response + Environment.NewLine;
						currentState = DialogState.Answering;
					}

					return true;
				}
				case DialogState.Cleansing:
				{
					StateLogging(loggingEnabled);
					currentState = DialogState.Awaiting;
					outputMessage = await CleansingAnswer(outputMessage);
					return true;
				}

				default:
					throw new Exception("Incorrect State");
			}
			throw new Exception("Incorrect ending");
		}

		private async Task<bool> SeparateQuestion(string message)
		{
			var separatedQuestionsString = await questionSeparatorDialog.SendMessage(message);

			if (string.IsNullOrEmpty(separatedQuestionsString))
			{
				throw new Exception("Пользователь не задал ни одного вопроса");
			}
			separatedQuestions = new Queue<string>( separatedQuestionsString.Split(new char[] { ';' }));//Небезопасно если в ходе текста встретится такой символ
			questionSeparatorDialog.ClearDialog(false);
			return true;
		}
		private async Task<string> CleansingAnswer(string message)
		{
			var cleanisingAnswer = await cleansingDialog.SendMessage(message);

			if (string.IsNullOrEmpty(cleanisingAnswer))
			{
				throw new Exception("Не могу почистить");
			}
			cleansingDialog.ClearDialog(false);
			return cleanisingAnswer;
		}
		private async Task<HelperResponse> GetAnswer(string userPrompt)
		{
			var sourceTypeIndexes = await GetSourcesTypeByUserPrompt(userPrompt);
			if (sourceTypeIndexes.Count == 0)
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
					userDialog.ReplaceSystemPrompt(message: GetSystemPrompt(subjectArea, null), clearDialog: false);
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
					userDialog.ReplaceSystemPrompt(message: GetSystemPrompt(subjectArea, sources), clearDialog: false);
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

			
			classificationDialog.ClearDialog();
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

			availableTypesAndFileNames.Add("Неопределено", String.Empty);


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

		//Move to ILogger
		public void StateLogging(bool enabled)
		{
			if (enabled)
			{
				Console.ForegroundColor = ConsoleColor.Blue;
				Console.WriteLine("Current state is " + currentState.ToString());
				Console.ResetColor();
			}
		}

		#region Propmpts

		//тоже переопределить в JSON объект с флагом что нужно уточнять, полем для уточнения и полем для ответа
		private static string GetSystemPrompt(string subjectArea, List<string>? sources, string? additionalInstructions = "") //TODO
		{
			var systemPrompt = $"Вы являетесь интеллектуальным помощником, обученным на базе знаний по теме: {subjectArea}." +
				$"Ваша задача – предоставлять точные, лаконичные и понятные ответы пользователям на основе информации из базы знаний." +
				$"\r\nЕсли пользовательский вопрос имеет однозначный ответ в базе знаний, предоставьте его. " +
				$"\r\nЕсли тебе не хватает знаний ответить на вопрос, скажи: Мне не хватает знаний ответить на это" +
				$"\r\nИзбегайте предположений, не подтвержденных содержимым базы знаний. Не врите не выдумывайте" +
				$"\r\nОтвечайте в профессиональном и дружелюбном тоне." +
				$" Если пользователь задал вопрос не по теме, то отвечай что это не входит в твои обязанности." +
				"\r\n\r\nОтвет должен быть предоставлен в формате JSON со следующей структурой:" +
				"\"{ needСlarification\": <true если тебе хватает знаний ответить на вопрос или вопрос не по теме; false если тебе необходимо уточнение>," +
				"\"response\": \"<ответ пользователелю (пустое, если надо уточнять)>\"," +
				"\"clarificationQuestion\": \"<уточняющий вопрос, который необходимо передать пользователю (пустое, если уточнения не нужны)> }" +
				$"\r\nТвоя база знаний: ";

			if (sources is null)
			{
				systemPrompt += "Твоя база знаний пуста, поскольку пользователь задал нейтральный вопрос, ответь на него исходя из текущего текста, не выдумывай ничего лишнего";

			}
			else
			{
				foreach (var source in sources)
				{
					systemPrompt += $"{Environment.NewLine}{source}";
				}
			}
			return systemPrompt;
		}

		private static string GetClassificationPrompt(List<string> types)
		{
			var index = 0;
			var classificationPrompt =
				"Ты выступаешь в роли классификатора, котрый будет определять тип сообщения от пользователя на основе текущего и предыдущего ответов." +
				$" Твой ответ должен содержать только цифры, разделенные символом ;" +
				$" Не выходи за рамки этих ответов, если пользователь спрашивает что-то выходящее за рамки предметной области, напиши -1.";

			foreach (var type in types)
			{
				var typeDescription = $"{Environment.NewLine}Если пользовательский запрос относится к теме {type}, напиши {index}";

				classificationPrompt += typeDescription;
				index++;
			}
			classificationPrompt += $"Если вопрос относится к нейтральной катерогии, например привестствие, прощание, благодарности, вопросы о том кто ты, напиши только: {types.Count}";

			return classificationPrompt;
		}

		private static string GetClarifyingPrompt()
		{
			// пока просто добавить subjectArea, чтобы вопрос хоть в теме был
			return "Если предоставленных пользователем данных недостаточно для выполнения запроса или запрос содержит неоднозначности, " +
				"ты должен задать уточняющий вопрос, чтобы уточнить детали или запросить недостающую информацию." +
				"Не пытайся угадывать или делать предположения без достаточных оснований." +
				"Твои уточняющие вопросы должны быть:" +
				"\r\nКраткими и ясными." +
				"\r\nНацеленными на получение информации, необходимой для точного выполнения запроса." +
				"\r\nУместными и по существу, исходя из предоставленных данных." +
				"\r\n\r\nОтвет должен быть предоставлен в формате JSON со следующей структурой:" +
				"{ \"originalMessage\": \"<текст, предоставленный пользователем>\"," +
				"\"needСlarification\": <true или false>," +
				"\"finalMessage\": \"<итоговое сообщение, содержащее оригинальное сообщение с уточнениями, если таковые есть (пустое, если надо уточнять)>\"," +
				"\"clarificationQuestion\": \"<уточняющий вопрос, который необходимо передать пользователю (пустое, если уточнения не нужны)> }";
		}

		private static string GetСleansingPrompt()
		{
			return "Тебе предоставлен список ответов на разные вопросы. Твоя задача:" +
				"\r\nОбъединить все ответы в единый текст без повторений." +
				"\r\nСоставить ответ так, чтобы он был сжатым, логичным и емким." +
				"\r\nНикакую информацию из предоставленных ответов нельзя пропускать или выкидывать." +
				"\r\nНе добавляй информацию от себя, не придумывай и не интерпретируй данные иначе, чем они представлены." +
				"\r\nСохраняй последовательность и структуру, делая текст максимально понятным и компактным для читателя.";
		}
		private static string GetQuestionSeparatingPrompt()
		{
			return "Ты должен проанализировать текст, который я тебе предоставлю, чтобы выявить все вопросы. Вопросы должны быть:" +
				"\r\nПолными и самодостаточными — каждый вопрос должен содержать всю необходимую информацию и не ссылаться на другие вопросы." +
				"\r\nЛогически разделенными — если вопросы похожи или ссылаются друг на друга, ты можешь объединить их в один." +
				"\r\nСохраненными в том порядке, в котором они представлены пользователем." +
				"\r\nРазделенными точкой с запятой (;) в итоговом списке. Если в тексте нет вопросов, просто верни пустую строку." +
				"\r\nНе добавляй ничего от себя.";
		}
		#endregion
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

	public class ClarifyingResponse
	{
		public string? OriginalMessage { get; set; }
		public string? ClarificationQuestion { get; set; }
		public string? FinalMessage { get; set; }
		public bool NeedСlarification { get; set; }
	}

	public enum DialogState
	{
		Awaiting,
		Separating,
		Clarifying,
		Answering,
		Cleansing,
		Error
	}
}

//case DialogState.Clarifying://возможно стоит объединить с системным промптом, запускать его если основной бот скажет что ему не хватает информации
//    StateLogging(loggingEnabled);
//    if (questionToClarify is not null)
//    {
//        var clarifyResult = await ClarifyingProcess(message);

//        if (clarifyResult.NeedСlarification)
//        {
//            clarifyingDialog.ClearDialog(false);
//            return clarifyResult.ClarificationQuestion!;
//        }
//        else
//        {
//            questionsToAnswer.Enqueue(clarifyResult.FinalMessage!);
//            break;
//        }
//    }

//    if (separatedQuestions!.Count == 0)
//    {
//        currentState = DialogState.Answering;
//        return await Process(message);
//    }
//    else
//    {
//        var questionToClarify = separatedQuestions.Dequeue();
//        var clarifyResult = await ClarifyingProcess(questionToClarify);

//        if (clarifyResult.NeedСlarification)
//        {
//            return clarifyResult.ClarificationQuestion!;
//        }
//        else
//        {
//            questionsToAnswer.Enqueue(clarifyResult.FinalMessage!);
//            break;
//        }

//    }