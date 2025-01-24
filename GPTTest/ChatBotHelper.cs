using System;
using System.Text.Json;
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
			List<string> filePaths)
		{
			this.classificationDialog = GetChatDialogProvider(providerType);
			this.userDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog = GetChatDialogProvider(providerType);
			this.clarifyingDialog = GetChatDialogProvider(providerType);
			this.questionSeparatorDialog = GetChatDialogProvider(providerType);

			this.cleansingDialog.SetSystemPrompt(message: GetClarifyingPrompt());
			this.questionSeparatorDialog.SetSystemPrompt(message: GetQuestionSeparatorPrompt());
			this.clarifyingDialog.SetSystemPrompt(message: GetClarifyingPrompt());

			availableTypesAndFileNames = GetAvailableTypesAndFileNames(filePaths);

			currentState = DialogState.Awaiting;
		}

		private DialogState currentState;


		private Queue<string>? separatedQuestions = null;
		private string? questionToClarify = null;

		private Queue<string> questionsToAnswer = new Queue<string>();

		private List<int>? lastSourceTypeIndexes = null;

		public async Task<string> Process(string message)
		{
			switch (currentState)
			{
				case DialogState.Awaiting:
					StateLogging();
					currentState = DialogState.Separating;
					return await Process(message);
				case DialogState.Separating:
					StateLogging();
					var result = await SeparateQuestion(message); //TODO Handle this bool value
					return await Process(message);
				case DialogState.Clarifying:
					StateLogging();
					if (questionToClarify is not null)
					{
						var clarifyResult = await ClarifyingProcess(message);

						if (clarifyResult.NeedСlarification)
						{
							clarifyingDialog.ClearDialog(false);
							return clarifyResult.ClarificationQuestion!;
						}
						else
						{
							questionsToAnswer.Enqueue(clarifyResult.FinalMessage!);
							break;
						}
					}

					if (separatedQuestions!.Count == 0)
					{
						currentState = DialogState.Answering;
						return await Process(message);
					}
					else
					{
						var questionToClarify = separatedQuestions.Dequeue();
						var clarifyResult = await ClarifyingProcess(questionToClarify);

						if (clarifyResult.NeedСlarification)
						{
							return clarifyResult.ClarificationQuestion!;
						}
						else
						{
							questionsToAnswer.Enqueue(clarifyResult.FinalMessage!);
							break;
						}

					}
				case DialogState.Answering:
					StateLogging();
					var answeringResult = "";

					if (questionsToAnswer.Count == 1)
					{
						currentState = DialogState.Cleansing;
					}
					else
					{
						currentState = DialogState.Awaiting;
					}

					while(questionsToAnswer.Count == 0)
					{
						var questionToAnswer = questionsToAnswer.Dequeue();
						var partialAnswer = await GetAnswer(questionToAnswer);

						answeringResult += partialAnswer + Environment.NewLine;
					}
					return answeringResult;
				case DialogState.Cleansing:
					StateLogging();
					currentState = DialogState.Awaiting;
					return await CleansingAnswer("");

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

			currentState = DialogState.Clarifying;
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
		private async Task<ClarifyingResponse> ClarifyingProcess(string questionToClarify)
		{
			var clarifyingResultString = await clarifyingDialog.SendMessage(questionToClarify);
			var clarifyingResult = JsonSerializer.Deserialize<ClarifyingResponse>(clarifyingResultString);

			if (clarifyingResult == null )
			{
				throw new Exception("Can't get clarification");
			}

			return clarifyingResult;
		}
		private async Task<string> GetAnswer(string userPrompt)
		{
			var sourceTypeIndexes = await GetSourcesTypeByUserPrompt(userPrompt);
			if (sourceTypeIndexes.Count == 0)
			{
				return "Некорректный запрос";
			}

			var isEqual = lastSourceTypeIndexes?.SequenceEqual<int>(sourceTypeIndexes) ?? false;

			if (!isEqual)
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
				userDialog.ReplaceSystemPrompt(message: GetSystemPrompt(sources), clearDialog: false);
				lastSourceTypeIndexes = sourceTypeIndexes;
			}

			return await userDialog.SendMessage(userPrompt);
		}
		private async Task<List<int>> GetSourcesTypeByUserPrompt(string userPrompt)
		{
			if (AvailableTypes.Count == 0)
			{
				throw new Exception("Types dont exist");
			}
			classificationDialog.SetSystemPrompt(message: GetClassificationPrompt(AvailableTypes));
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

		public void StateLogging(bool active = true)
		{
			Console.ForegroundColor = ConsoleColor.Blue;
			Console.WriteLine("Current state is " + currentState.ToString());
			Console.ResetColor();
		}

		#region Propmpts
		private static string GetSystemPrompt(List<string> sources)
		{
			var systemPrompt =
				$"Я хочу чтобы ты выступил в роле консультанта магазина по продаже рукодельных ножей определенным вопросом, ты не должен отвечать на все подряд." +
				$" Для ответов пользователям используй только предоставленную базу знаний, не выдумывай ничего лишнего, если тебе не хватает знаний ответить на вопрос, скажи:" +
				$"\"Мне не хватает знаний ответить на это\"." +
				$" Если пользователь задал вопрос не по теме, то отвечай что это не входит в твои обязанности." +
				$" Твоя база знаний:";

			foreach (var source in sources)
			{
				systemPrompt += $"{Environment.NewLine}{source}";
			}

			return systemPrompt;
		}

		private static string GetClassificationPrompt(List<string> types)
		{
			var index = 0;
			var classificationPrompt =
				"Ты выступаешь в роли классификатора, котрый будет определять тип сообщения от пользователя на основе текущего и предыдущего ответов." +
				$" Твой ответ должен содержать только одну цифру." +
				$" Не выходи за рамки этих ответов, если пользователь спрашивает что-то выходящее за рамки предметной области, напиши -1.";

			foreach (var type in types)
			{
				var typeDescription = $"{Environment.NewLine}Если пользовательский запрос относится к теме {type}, напиши {index}";

				classificationPrompt += typeDescription;
				index++;
			}
			return classificationPrompt;
		}

		private static string GetClarifyingPrompt()
		{
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
		private static string GetQuestionSeparatorPrompt()
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
