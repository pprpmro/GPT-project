using GPTProject.Providers.Data;
using static GPTProject.Providers.Configurations.DialogConfigurations.ChatGPT;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class ChatGPTDialog : BaseChatDialog<Message, Request>
	{
		public ChatGPTDialog(int maxDialogHistorySize = 50)
			: base (DialogModels.GPT_4o_Mini, CompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxDialogHistorySize = maxDialogHistorySize;
		}
	}
}
