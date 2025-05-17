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
				ProviderType.DefaultDialog => CreateDefaultProvider("darkidol-llama-3.1-8b-instruct-1.2-uncensored@q8_0"),
				_ => throw new NotImplementedException()
			};

			return dialog;
		}

		public IChatDialog CreateDefaultProvider(string modelName, string competitionEndpoint = "http://localhost:1234/v1/chat/completions")
		{
			return new DefaultGPTDialog(modelName, competitionEndpoint);
		}
	}
}