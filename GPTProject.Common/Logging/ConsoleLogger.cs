namespace GPTProject.Common.Logging
{
	public class ConsoleLogger : ILogger
	{
		public void Log(string message, LogLevel level)
		{
			Console.ForegroundColor = level switch
			{
				LogLevel.Warning => ConsoleColor.Yellow,
				LogLevel.Error or LogLevel.Danger => ConsoleColor.Red,
				_ => ConsoleColor.Blue
			};

			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
			Console.ResetColor();
		}
	}
}
