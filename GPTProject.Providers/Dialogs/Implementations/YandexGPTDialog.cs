using GPTProject.Providers.Data.Dialogs;
using static GPTProject.Providers.Common.Configurations.YandexGPT;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class YandexGPTDialog : BaseChatDialog<Message, Request>
	{
		public YandexGPTDialog() : this(10000) { }

		public YandexGPTDialog(int maxTokenHistorySize = 10000)
			: base(DialogModels.Lite, OpenAILikeDialogCompletionsEndpoint)
		{
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
			MaxTokenHistorySize = maxTokenHistorySize;
		}
	}
}
