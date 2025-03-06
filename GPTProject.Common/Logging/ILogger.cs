namespace GPTProject.Common.Logging
{
	public interface ILogger
	{
		public void Log(string message, LogLevel level = LogLevel.Warning);
	}
}
