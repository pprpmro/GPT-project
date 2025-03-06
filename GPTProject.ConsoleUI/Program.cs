using GPTProject.Core.Providers;
using GPTProject.Common.Logging;
using GPTProject.Core.ChatBot;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var subjectArea = "Эксперт по некоторым динозаврам, отвечаю на вопросы о их видах, жизни и особенностях.";
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			string segmentsFolderPath = Path.Combine(sourcesFolderPath, "Segments");
			string metadataFolderPath = Path.Combine(sourcesFolderPath, "Metadata");
			List<string> segmentFiles = FileHelper.GetTxtFiles(segmentsFolderPath);
			List<string> metadataFiles = FileHelper.GetTxtFiles(metadataFolderPath);
			var knowledgeBaseFiles = new KnowledgeBaseFiles() { SegmentPaths = segmentFiles, MetadataPaths = metadataFiles };

			ILogger logger = new ConsoleLogger();
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

			try
			{
				await RunChatBot(helper);
			}
			catch (Exception ex)
			{
				logger.Log($"Exception: {ex.Message}", LogLevel.Error);
			}
		}

		private static async Task RunChatBot(Agent helper)
		{
			while (true)
			{
				if (helper.DialogState is DialogState.Waiting or DialogState.Clarifying)
				{
					var userMessage = GetUserMessage();
					helper.SetCurrentUserMessage(userMessage);
				}

				bool success = await helper.Process();
				if (!success)
				{
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Ошибка обработки запроса.");
					Console.ResetColor();
					continue;
				}

				if (helper.DialogState is DialogState.Waiting or DialogState.Clarifying or DialogState.Error)
				{
					Console.WriteLine(helper.GetOutputMessage());
				}
			}
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

		public static class FileHelper
		{
			public static List<string> GetTxtFiles(string folderPath)
			{
				if (!Directory.Exists(folderPath))
				{
					Console.WriteLine($"Папка {folderPath} не найдена.");
					return new List<string>();
				}
				return Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly).ToList();
			}
		}
	}
}
