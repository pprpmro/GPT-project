using GPTProject.Providers.Data;
using static GPTProject.Providers.Config.Settings.ChatGPT;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class ChatGPTDialog : BaseChatDialog<Message, Request>
	{
		public ChatGPTDialog(int maxDialogHistorySize = 50)
			: base (Model, CompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxDialogHistorySize = maxDialogHistorySize;
			TotalSendedCharacterCount = 0;
		}
	}
}
