using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.DeepSeek;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class DeepSeekDialog : BaseChatDialog<Message, Request>
	{
		public DeepSeekDialog(int maxDialogHistorySize = 50)
			: base(DialogModels.Default, DialogCompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxDialogHistorySize = maxDialogHistorySize;
		}
	}
}
