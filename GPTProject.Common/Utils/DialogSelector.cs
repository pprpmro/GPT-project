using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Factories.Implementations;
using GPTProject.Providers.Factories.Interfaces;

namespace GPTProject.Common.Utils
{
	public static class DialogSelector
	{
		private static readonly IDialogFactory dialogFactory = new DialogFactory();
		public static IChatDialog GetDialog(Dictionary<DialogType, ProviderType> providerTypes, DialogType dialogType)
		{
			if (!providerTypes.TryGetValue(dialogType, out var providerType))
			{
				throw new ArgumentException($"Поставщик для {dialogType} не задан в providerTypes.");
			}

			return dialogFactory.Create(providerType);
		}
	}
}
