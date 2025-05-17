using GPTProject.Common;
using GPTProject.Common.Logging;
using GPTProject.Core.ChatBot;
using GPTProject.Core.ChatBot.LLMMemory;
using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Factories.Implementations;
using GPTProject.Providers.Factories.Interfaces;
using static GPTProject.Providers.Common.Configurations;

namespace GPTProject.ConsoleUI
{
	public partial class Program
	{
		private static readonly ILogger logger = new ConsoleLogger();
		private static readonly IVectorizerFactory vectorizerFactory = new VectorizerFactory();
		private static readonly IDialogFactory dialogFactory = new DialogFactory();

		private static readonly ManualResetEventSlim shutdownEvent = new(false);

		const string subjectArea = "Эксперт по некоторым динозаврам, отвечаю на вопросы о их видах, жизни и особенностях. В твоей базе знаний есть информация о следующих видах:" +
				"алиорам\r\n" +
				"аллозавр\r\n" +
				"орнитолест\r\n" +
				"трицератопс\r\n" +
				"целофизис\r\n" +
				"целюр\r\n" +
				"Другая информация тебе не доступна. Ты ничего не знаешь про других, не выдавай м не выдумывай ничего про них\n" +
				"Если ты будешь обманывать или выдумывать, то пользователь очень сильно расстроится!!!\n";

		private static async Task RunClassicChatBot(IChatDialog chatDialog)
		{
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			string segmentsFolderPath = Path.Combine(sourcesFolderPath, "Segments");
			List<string> segmentFiles = TxtFileHelper.GetListTxtFilePaths(segmentsFolderPath);
			var systemPrompt = PromptManager.GetClassicSystemPrompt(subjectArea, TxtFileHelper.GetListTxtFileText(segmentFiles));
			chatDialog.UpdateSystemPrompt(systemPrompt, clearDialog: true);

			logger.Log("Чат-бот запущен. Введите 'exit' для выхода.", LogLevel.Info);

			while (true)
			{
				Console.Write("\nВведите сообщение: ");
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

				var response = await chatDialog.SendMessage(userInput, onStreamedData: null, stream: false, rememberMessage: true);
				Console.WriteLine($"Бот: {response}");
			}
			logger.Log($"Потрачено на диалог: {chatDialog.SessionTokenUsage}", LogLevel.Error);

		}

		private static async Task RunClassicChatBot()
		{
			IChatDialog chatDialog = new GigaChatDialog(20000);
			try
			{
				await RunClassicChatBot(chatDialog);
			}
			catch (Exception ex)
			{
				logger.Log($"Exception: {ex.Message}", LogLevel.Error);
			}
		}

		private static Agent CreateAgent()
		{
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			string segmentsFolderPath = Path.Combine(sourcesFolderPath, "Segments");
			string metadataFolderPath = Path.Combine(sourcesFolderPath, "Metadata");
			List<string> segmentFiles = TxtFileHelper.GetListTxtFilePaths(segmentsFolderPath);
			List<string> metadataFiles = TxtFileHelper.GetListTxtFilePaths(metadataFolderPath);
			var knowledgeBaseFiles = new KnowledgeBaseFiles() { SegmentPaths = segmentFiles, MetadataPaths = metadataFiles };

			var providerConfig = new Dictionary<DialogType, ProviderType>
			{
				{ DialogType.User, ProviderType.YandexGPT },
				{ DialogType.Classification, ProviderType.YandexGPT },
				{ DialogType.Cleansing, ProviderType.YandexGPT },
				{ DialogType.QuestionSeparator, ProviderType.YandexGPT },
				{ DialogType.SmallTalk, ProviderType.YandexGPT }
			};
			var helper = new Agent(providerConfig, subjectArea, knowledgeBaseFiles, logger);
			logger.Log("ChatBotHelper готов к работе", LogLevel.Info);

			return helper;
		}

		private static async Task RunAgent()
		{
			var agent = CreateAgent();
			try
			{
				await agent.Run(() => Task.FromResult(GetUserMessage()));
			}
			catch (Exception ex)
			{
				logger.Log($"Exception: {ex.Message}", LogLevel.Danger);
			}
		}

		private static async Task RunMemoryAgent()
		{
			var request = new VectorizerRequest()
			{
				Encoding_format = "float"
			};

			var factory = new VectorizerFactory();
			var vectorizer = factory.Create(ChatGPT.EmbeddingModels.Small);

			var providerConfig = new Dictionary<DialogType, ProviderType>
			{
				{ DialogType.User, ProviderType.ChatGPT },
				{ DialogType.Saving, ProviderType.ChatGPT },
				{ DialogType.Restoring, ProviderType.ChatGPT }
			};
			var dialogue = new DialogueAgent(providerConfig, "cat", request, vectorizer, "Представь что ты котик и веди себя соответствующе");
			AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
			{
				Console.WriteLine("\nProcessExit: Приложение закрывается...");
				SaveAsync(dialogue).Wait();
			};
			await dialogue.Run(() => Task.FromResult(GetUserMessage()), Console.Write);
		}

		private static async Task RunVectorizer()
		{
			var request = new VectorizerRequest()
			{
				Encoding_format = "float",
				Input = [ "Пёс" ]
			};

			var vectorizer = vectorizerFactory.Create(GigaChat.EmbeddingModels.Default);
			var testVector = await vectorizer.GetEmbeddingAsync(request);

			Console.WriteLine(testVector);
		}

		static async Task SaveAsync(DialogueAgent agent)
		{
			try
			{
				await agent.Save();
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при сохранении: {ex.Message}");
			}
			finally
			{
				shutdownEvent.Set(); // Сигнализируем, что завершение сохранения завершено
			}
		}

		private static string GetUserMessage()
		{
			Console.ForegroundColor = ConsoleColor.Yellow;
			Console.Write("\nВведите сообщение: ");
			string? userMessage;
			while (string.IsNullOrWhiteSpace(userMessage = Console.ReadLine()))
			{
				Console.Write("Сообщение не может быть пустым! Попробуйте снова: ");
			}
			Console.ResetColor();
			return userMessage;
		}

		public static async Task Main(string[] args)
		{
			//await MetricTest.Run();
			//return;

			//await RunClassicChatBot();
			//return;

			//await RunAgent();
			//return;

			//await RunMemoryAgent();
			//return;

			await RunVectorizer();
			return;
		}
	}
}
