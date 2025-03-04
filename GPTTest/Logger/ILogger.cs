namespace GPTProject.Core.Logger
{
	internal interface ILogger
	{
		public void Log(string message, LogLevel level = LogLevel.Warning);
	}
}
