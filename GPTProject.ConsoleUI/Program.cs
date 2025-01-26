using GPTProject.Core;
using Type = GPTProject.Core.Type;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		static void Main(string[] args)
		{
			var subjectArea = "Знаток о динозаврах, можешь рассказать о некоторых видах.";
			var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\..\"));
			string sourcesFolderPath = Path.Combine(startPath, @"GPTProject.ConsoleUI\Sources");
			List<string> txtFiles = GetTxtFiles(sourcesFolderPath);

			var helper = new ChatBotHelper(Type.ChatGPT, subjectArea, txtFiles);

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("ChatBotHelper готов к работе");
			Console.ResetColor();

			try
			{
				while (true)
				{
					if (helper.DialogState == DialogState.Awaiting ||
						helper.DialogState == DialogState.Clarifying)
					{
						var userMessage = "";
						userMessage = Console.ReadLine();

						Console.ForegroundColor = ConsoleColor.Yellow;
						while (userMessage != null || string.IsNullOrEmpty(userMessage))
						{
							Console.WriteLine("Введите сообщение!!!");
							userMessage = Console.ReadLine();

						}
						Console.ResetColor();
						var result = helper.Process(userMessage);
						Console.WriteLine(result);
					}
				}
			}
			catch (Exception ex)
			{
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine($"{typeof(Exception)} \n\rMessage: {ex.Message}");
				Console.ResetColor();
			}
		}

		static List<string> GetTxtFiles(string folderPath)
		{
			if (!Directory.Exists(folderPath))
			{
				Console.WriteLine($"Папка {folderPath} не найдена.");
				return new List<string>();
			}
			return new List<string>(Directory.GetFiles(folderPath, "*.txt", SearchOption.TopDirectoryOnly));
		}
	}
}
