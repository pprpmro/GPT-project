using System.Threading;

namespace GPTProject.Providers.Common
{
	public class LoadingIndicator
	{
		private CancellationTokenSource? cancellationTokenSource;
		private readonly Action<string> outputAction;

		private int lastLength = 0;

		public LoadingIndicator(Action<string> outputAction)
		{
			this.outputAction = outputAction;
		}

		public void Start()
		{
			cancellationTokenSource = new CancellationTokenSource();
			Task.Run(() => ShowLoading(cancellationTokenSource.Token));
		}

		public void Stop()
		{
			cancellationTokenSource?.Cancel();
			ClearOutput();
		}

		public async Task StopAsync()
		{
			if (cancellationTokenSource != null)
			{
				cancellationTokenSource.Cancel();
				await Task.Delay(100);
				ClearOutput();
			}
		}

		private async Task ShowLoading(CancellationToken cancellationToken)
		{
			string[] animationFrames = { ".", "..", "..." };
			int frameIndex = 0;

			while (!cancellationToken.IsCancellationRequested)
			{
				string frame = $"Thinking {animationFrames[frameIndex]} ";
				outputAction?.Invoke($"\r{frame}");
				lastLength = frame.Length;
				frameIndex = (frameIndex + 1) % animationFrames.Length;
				await Task.Delay(300);
			}
			ClearOutput();
		}

		private void ClearOutput()
		{
			string clearString = new string(' ', lastLength);
			outputAction?.Invoke($"\r{clearString}\r");
		}
	}
}
