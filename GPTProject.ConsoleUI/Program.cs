using GPTProject.Core;
using Type = GPTProject.Core.Type;

namespace GPTProject.ConsoleUI
{
	public class Program
	{
		static void Main(string[] args)
		{
			var helper = new ChatBotHelper(Type.ChatGPT, null);

			Console.ForegroundColor = ConsoleColor.Green;
			Console.WriteLine("Готов к работе");
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
	}
}
