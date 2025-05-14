using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.DeepSeek;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class DeepSeekDialog : BaseChatDialog<Message, Request>
	{
		public DeepSeekDialog() : this(10000) { }

		public DeepSeekDialog(int maxTokenHistorySize = 10000)
			: base(DialogModels.Default, DialogCompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxTokenHistorySize = maxTokenHistorySize;
		}
	}
}
