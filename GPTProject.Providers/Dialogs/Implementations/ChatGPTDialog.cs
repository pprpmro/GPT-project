using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.ChatGPT;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class ChatGPTDialog : BaseChatDialog<Message, Request>
	{
		public ChatGPTDialog() : this(10000) { }

		public ChatGPTDialog(int maxTokenHistorySize = 10000)
			: base (DialogModels.GPT_4o_Mini, DialogCompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxTokenHistorySize = maxTokenHistorySize;
		}
	}
}
