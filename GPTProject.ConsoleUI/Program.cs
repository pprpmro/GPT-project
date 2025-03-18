using GPTProject.Core.Providers;
using GPTProject.Core.ChatBot;
using GPTProject.Core.Interfaces;
using GPTProject.Core.Providers.ChatGPT;
using GPTProject.Common.Logging;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		static readonly ILogger logger = new ConsoleLogger();

		public static async Task Main(string[] args)
		{
			//IChatDialog chatDialog = new ChatGPTDialog(50);
			//try
			//{
			//	await RunClassicChatBot(chatDialog);
			//}
			//catch (Exception ex)
			//{
			//	logger.Log($"Exception: {ex.Message}", LogLevel.Error);
			//}


			var agent = CreateAgent();
			try
			{
				await RunSegmentChatBot(agent);
			}
			catch (Exception ex)
			{
				logger.Log($"Exception: {ex.Message}", LogLevel.Error);
			}
		}

		private static async Task RunClassicChatBot(IChatDialog chatDialog)
		{
			var subjectArea = "Эксперт по некоторым динозаврам, отвечаю на вопросы о их видах, жизни и особенностях.";
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			string segmentsFolderPath = Path.Combine(sourcesFolderPath, "Segments");
			List<string> segmentFiles = TxtFileHelper.GetListTxtFilePaths(segmentsFolderPath);
			var systemPrompt = PromptManager.GetClassicSystemPrompt(subjectArea, TxtFileHelper.GetListTxtFileText(segmentFiles));
			chatDialog.UpdateSystemPrompt(systemPrompt, clearDialog: true);

			logger.Log("Чат-бот запущен. Введите 'exit' для выхода.", LogLevel.Info);

			while (true)
			{
				Console.WriteLine("\nВы: ");
				var userInput = Console.ReadLine()?.Trim();

				if (string.IsNullOrEmpty(userInput))
				{
					Console.WriteLine("Введите сообщение.");
					continue;
				}

				if (userInput.Equals("exit", StringComparison.OrdinalIgnoreCase))
				{
					logger.Log("Завершение работы чат-бота...", LogLevel.Info);
					break;
				}

				var response = await chatDialog.SendMessage(userInput);
				Console.WriteLine($"Бот: {response}");
			}
			logger.Log($"Потрачего на диалог: {chatDialog.TotalSendedCharacterCount}", LogLevel.Error);
		}

		private static Agent CreateAgent()
		{
			var subjectArea = "Эксперт по некоторым динозаврам, отвечаю на вопросы о их видах, жизни и особенностях.";
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			string segmentsFolderPath = Path.Combine(sourcesFolderPath, "Segments");
			string metadataFolderPath = Path.Combine(sourcesFolderPath, "Metadata");
			List<string> segmentFiles = TxtFileHelper.GetListTxtFilePaths(segmentsFolderPath);
			List<string> metadataFiles = TxtFileHelper.GetListTxtFilePaths(metadataFolderPath);
			var knowledgeBaseFiles = new KnowledgeBaseFiles() { SegmentPaths = segmentFiles, MetadataPaths = metadataFiles };

			var providerConfig = new Dictionary<DialogType, ProviderType>
			{
				{ DialogType.User, ProviderType.ChatGPT },
				{ DialogType.Classification, ProviderType.ChatGPT },
				{ DialogType.Cleansing, ProviderType.ChatGPT },
				{ DialogType.QuestionSeparator, ProviderType.ChatGPT },
				{ DialogType.SmallTalk, ProviderType.ChatGPT }
			};
			var helper = new Agent(providerConfig, subjectArea, knowledgeBaseFiles, logger);
			logger.Log("ChatBotHelper готов к работе", LogLevel.Info);

			return helper;
		}
		private static async Task RunSegmentChatBot(Agent helper)
		{
			while (true)
			{
				if (helper.DialogState is DialogState.Waiting or DialogState.Clarifying)
				{
					var userMessage = GetUserMessage();
					if (userMessage.Equals("exit", StringComparison.OrdinalIgnoreCase))
					{
						logger.Log("Завершение работы чат-бота...", LogLevel.Info);
						break;
					}

					helper.SetCurrentUserMessage(userMessage);
				}


				bool success = await helper.Process();
				if (!success)
				{
					logger.Log("Ошибка обработки запроса. Переключение на состояние Waiting", LogLevel.Error);
					helper.SetWaitingState();
					continue;
				}

                if (helper.DialogState is DialogState.Clarifying)
                {
                    Console.WriteLine(helper.GetOutputQuestionMessage());
                }


                if (helper.DialogState is DialogState.Waiting or DialogState.Clarifying or DialogState.Error)
				{
					Console.WriteLine(helper.GetOutputMessage());
				}
			}
			logger.Log($"Потрачего на диалог: {helper.TotalSendedTokenCount}", LogLevel.Error);

		}
		private static string GetUserMessage()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("Введите сообщение: ");
			string? userMessage;
			while (string.IsNullOrWhiteSpace(userMessage = Console.ReadLine()))
			{
				Console.Write("Сообщение не может быть пустым! Попробуйте снова: ");
			}
			Console.ResetColor();
			return userMessage;
		}
		private static class TxtFileHelper
		{
			public static List<string> GetListTxtFilePaths(string folderPath)
			{
				if (!Directory.Exists(folderPath))
				{
					Console.WriteLine($"Папка {folderPath} не найдена.");
					return new List<string>();
				}
				return Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly).ToList();
			}

			public static List<string> GetListTxtFileText(List<string> segmentFiles)
			{
				var segments = new List<string>();

				foreach (var filePath in segmentFiles)
				{
					if (File.Exists(filePath))
					{
						segments.Add(File.ReadAllText(filePath));
					}
				}

				return segments;
			}
		}
	}
}
