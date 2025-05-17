using GPTProject.Providers.Common;
using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.GigaChat;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class GigaChatDialog : BaseChatDialog<Message, Request>
	{
		public GigaChatDialog() : this(10000) { }

		public GigaChatDialog(int maxTokenHistorySize = 10000)
			: base(DialogModels.Lite2, DialogEndpoint)
		{
			authentificator = new GigaChatAuthentificator(httpClient);
			MaxTokenHistorySize = maxTokenHistorySize;
		}

		private readonly GigaChatAuthentificator authentificator;

		public override async Task<string> SendMessage(string? content, Action<string>? onStreamedData = null, bool stream = true, bool rememberMessage = true)
		{
			var authenticated = await authentificator.EnsureAccessData();
			if (!authenticated)
			{
				throw new InvalidOperationException("Cant get access data");
			}
			return await base.SendMessage(content, onStreamedData, stream, rememberMessage);
		}
	}
}
