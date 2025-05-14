using GPTProject.Providers.Data.Dialogs;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class DefaultGPTDialog : BaseChatDialog<Message, Request>
	{
		public DefaultGPTDialog(string modelName, string completionsEndpoint)
			: base(modelName, completionsEndpoint) { }

		public DefaultGPTDialog(string modelName, string completionsEndpoint, string apiKey)
			: base(modelName, completionsEndpoint)
		{
			httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
		}

		public DefaultGPTDialog(string modelName, string completionsEndpoint, HttpClient client)
			: base(modelName, completionsEndpoint)
		{
			httpClient = client;
		}
	}
}
