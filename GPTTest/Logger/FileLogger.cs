namespace GPTProject.Core.Logger
{
	public class FileLogger : ILogger
	{
		private readonly string _filePath;
		private readonly SemaphoreSlim _lock = new(1, 1);

		public FileLogger(string filePath)
		{
			_filePath = filePath;
		}

		public async void Log(string message, LogLevel level)
		{
			var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";

			await _lock.WaitAsync();
			try
			{
				await File.AppendAllTextAsync(_filePath, logMessage + Environment.NewLine);
			}
			finally
			{
				_lock.Release();
			}
		}
	}
}