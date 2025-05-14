using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.ChatGPT;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class ChatGPTDialog : BaseChatDialog<Message, Request>
	{
		public ChatGPTDialog(int maxDialogHistorySize = 50)
			: base (DialogModels.GPT_4o_Mini, DialogCompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxDialogHistorySize = maxDialogHistorySize;
		}
	}
}
