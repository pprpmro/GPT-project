using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Providers.Factories.Interfaces
{
	public interface IDialogFactory
	{
		IChatDialog Create(ProviderType providerType);
		IChatDialog CreateDefaultProvider(string modelName, string competitionEndpoint);
	}
}
