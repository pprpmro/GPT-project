using GPTProject.Core.ChatBot;
using GPTProject.Common.Logging;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Dialogs;
using GPTProject.Testing.Metrics;
using GPTProject.Testing.Evaluation;
using GPTProject.Providers.Vectorizers.Implementation;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Core.ChatBot.LLMMemory;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		static readonly ILogger logger = new ConsoleLogger();

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
			logger.Log($"Потрачено на диалог: {chatDialog.TotalSendedCharacterCount}", LogLevel.Error);

		}

		public static async Task Main(string[] args)
		{
			//await MetricsTest();
			//return;
			//return;
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
				await agent.Run(() => Task.FromResult(GetUserMessage()));
			}
			catch (Exception ex)
			{
				logger.Log($"Exception: {ex.Message}", LogLevel.Error);
			}

			var request = new VectorizerRequest()
			{
				Encoding_format = "float",
				Model = "text-embedding-3-small",
			};

			var dialogue = new DialogueAgent(new ChatGPTDialog(), "cat", request, "Представь что ты котик и веди себя соответствующе");
			await dialogue.Run(() => Task.FromResult(GetUserMessage()), Console.WriteLine);
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

		private static async Task MetricsTest()
		{
			var segmentItems = new List<TestItem>
			{
				new TestItem
				{
					Question = "В каком периоде жил трицератопс?",
					Generated = "Трицератопс жил в самом конце мелового периода, около 68–66 миллионов лет назад.",
					Reference = "Поздний меловой период, 68–66 млн лет назад."
				},
				new TestItem
				{
					Question = "Какой была масса взрослой особи аллозавра?",
					Generated = "Информация о массе взрослой особи аллозавра отсутствует в предоставленной базе знаний.",
					Reference = "Информация о массе отсутствует в базе знаний."
				},
				new TestItem
				{
					Question = "Чем питался орнитолест?",
					Generated = "Орнитолестес, вероятно, питался мелкими наземными позвоночными, такими как млекопитающие, ящерицы, лягушки, саламандры и птенцы динозавров. Также предполагалось, что он мог ловить рыбу, используя свои конические передние зубы. Некоторые исследования указывают на возможность охоты на более крупную добычу, такую как орнитоподы размером с камптозавра.",
					Reference = "Питался мелкими позвоночными и, возможно, рыбой."
				},
				new TestItem
				{
					Question = "Сравните размеры алиорама и аллозавра.",
					Generated = "Алиорам достигал длины от 5 до 6 метров, в то время как аллозавр достигал длины около 7,5 метров.",
					Reference = "Длинна алиорамы — 5–6 м, аллозавра — около 7,5 м."
				},
				new TestItem
				{
					Question = "Чем питался аллозавр и в какой период он жил?",
					Generated = "Аллозавр, живший в позднем юрском периоде примерно 155–145 миллионов лет назад, был хищником, который, вероятно, охотился на крупных динозавров, таких как завроподы, действуя в группах. Существуют также свидетельства каннибализма среди аллозавров.",
					Reference = "Аллозавр жил в позднем юрском периоде, хищник, охотился на завроподов, существуют свидетельства каннибализма"
				},
				new TestItem
				{
					Question = "Сравни трицератопса, алиорама и орнитолеста по размеру и питанию.",
					Generated = "Трицератопс был самым крупным из трех, достигая длины 8–9 метров и веса до 12 тонн, питался растительностью. Алиорам имел длину от 5 до 6 метров и был хищником, охотившимся на мелких животных. Орнитолестес был самым маленьким, длиной около 2 метров, и также был хищником, кормящимся мелкими позвоночными.",
					Reference = "Трицератопс — крупный травоядный, алиорам и орнитолест — хищники меньшего размера."
				}
			};

			var classicItems = new List<TestItem>
			{
				new TestItem
				{
					Question = "В каком периоде жил трицератопс?",
					Generated = "Трицератопс жил в самом конце мелового периода, около 68–66 миллионов лет назад.",
					Reference = "Поздний меловой период, 68–66 млн лет назад."
				},
				new TestItem
				{
					Question = "Какой была масса взрослой особи аллозавра?",
					Generated = "Масса взрослой особи аллозавра составляла приблизительно 1,5 тонны.",
					Reference = "Информация о массе отсутствует в базе знаний."
				},
				new TestItem
				{
					Question = "Чем питался орнитолест?",
					Generated = "Орнитолест, вероятно, питался мелкими наземными позвоночными, такими как млекопитающие, ящерицы, лягушки, саламандры и ринхоцефалы. Также высказывались предположения, что он мог ловить и есть более крупную добычу, чем другие мелкие тероподы, например, орнитоподов размером с камптозавра. Грегори С. Пол даже предполагал, что орнитолест мог использовать конические передние зубы для ловли рыбы.",
					Reference = "Питался мелкими позвоночными и, возможно, рыбой."
				},
				new TestItem
				{
					Question = "Сравните размеры алиорама и аллозавра.",
					Generated = "Алиорам достигал длины от 5 до 6 метров, в то время как аллозавр, как показал образец 'Big Al', был длиной около 7,5 метров. Таким образом, аллозавр был больше алиорама.",
					Reference = "Длинна алиорамы — 5–6 м, аллозавра — около 7,5 м."
				},
				new TestItem
				{
					Question = "Чем питался аллозавр и в какой период он жил?",
					Generated = "Аллозавр питался крупными динозаврами, охотясь на них, и, вероятно, охотился группами. Он жил в позднем юрском периоде, около 155-145 миллионов лет назад.",
					Reference = "Аллозавр жил в позднем юрском периоде, хищник, охотился на завроподов, существуют свидетельства каннибализма"
				},
				new TestItem
				{
					Question = "Сравни трицератопса, алиорама и орнитолеста по размеру и питанию.",
					Generated = "Трицератопс, алиорам и орнитолест значительно различаются по размеру и типу питания.\n\n1. **Размер:**\n- **Трицератопс**: Один из самых крупных представителей семейства цератопсид, длина достигала 8-9 метров, высота до 3 метров и весил 6-12 тонн.\n- **Алиорам**: Достигал длины от 5 до 6 метров, если взрослые особи были больше, то размеры могли быть и крупнее.\n- **Орнитолест**: Небольшой динозавр, длина около 2 метров.\n\n2. **Питание:**\n- **Трицератопс**: Растительноядный динозавр...\n- **Алиорам**: Хищный динозавр...\n- **Орнитолест**: Хищник...",
					Reference = "Трицератопс — крупный травоядный, алиорам и орнитолест — хищники меньшего размера."
				}
			};

			var runner = new Runner
			(
				new BERTScore(),
				new SemanticSimilarityScore(new DefaultVectorizer()),
				new LLMAsJudgeScore(new ChatGPTDialog())
			);

			var resultSeg = await runner.EvaluateAsync(segmentItems);
			Console.WriteLine("Segment score result");
			Console.WriteLine(resultSeg);

			Console.WriteLine();

			var resultClas = await runner.EvaluateAsync(classicItems);
			Console.WriteLine("Classic Score Result");
			Console.WriteLine(resultClas);
		}
	}
}
