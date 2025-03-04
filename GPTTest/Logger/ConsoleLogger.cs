using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTProject.Core.Logger
{
	public class ConsoleLogger : ILogger
	{
		public void Log(string message, LogLevel level)
		{
			Console.ForegroundColor = level switch
			{
				LogLevel.Warning => ConsoleColor.Yellow,
				LogLevel.Error => ConsoleColor.Red,
				_ => ConsoleColor.Blue
			};

			Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] [{level}] {message}");
			Console.ResetColor();
		}
	}
}
