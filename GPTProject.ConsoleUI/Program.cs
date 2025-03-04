using GPTProject.Core;
using GPTProject.Core.Logger;
using Type = GPTProject.Core.Type;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		public static async Task Main(string[] args)
		{
			var subjectArea = "Знаток о динозаврах, можешь рассказать о некоторых видах.";
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			List<string> txtFiles = FileHelper.GetTxtFiles(sourcesFolderPath);

			ILogger logger = new ConsoleLogger();
			var helper = new ChatBotHelper(Type.ChatGPT, subjectArea, txtFiles, logger);

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

		private static async Task RunChatBot(ChatBotHelper helper)
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

				if (helper.DialogState is DialogState.Waiting or DialogState.Clarifying)
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
