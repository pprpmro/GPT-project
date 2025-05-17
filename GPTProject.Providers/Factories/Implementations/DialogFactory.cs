using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Factories.Interfaces;

namespace GPTProject.Providers.Factories.Implementations
{
	public class DialogFactory : IDialogFactory
	{
		public IChatDialog Create(ProviderType providerType)
		{
			IChatDialog dialog = providerType switch
			{
				ProviderType.ChatGPT => new ChatGPTDialog(),
				ProviderType.GigaChat => new GigaChatDialog(),
				ProviderType.DeepSeek => new DeepSeekDialog(),
				ProviderType.YandexGPT => new DeepSeekDialog(),
				ProviderType.DefaultDialog => new DefaultGPTDialog("",""),
				_ => throw new NotImplementedException()
			};

			return dialog;
		}

		public IChatDialog CreateDefaultProvider(string modelName, string competitionEndpoint)
		{
			return new DefaultGPTDialog(modelName, competitionEndpoint);
		}
	}
}