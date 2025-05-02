using GPTProject.Providers.Data;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class DefaultGPTDialog : BaseChatDialog<Message, Request>
	{
		public DefaultGPTDialog(string modelName, string completionsEndpoint)
			: base(modelName, completionsEndpoint) { }
	}
}
