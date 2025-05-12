using GPTProject.Providers.Dialogs;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Common.Utils
{
	public static class DialogSelector
	{
		public static IChatDialog GetDialog(Dictionary<DialogType, ProviderType> providerTypes, DialogType dialogType)
		{
			if (!providerTypes.TryGetValue(dialogType, out var providerType))
			{
				throw new ArgumentException($"Поставщик для {dialogType} не задан в providerTypes.");
			}

			return GetChatDialogProvider(providerType);
		}

		private static IChatDialog GetChatDialogProvider(ProviderType type) => type switch
		{
			ProviderType.ChatGPT => new ChatGPTDialog(),
			ProviderType.GigaChat => new GigaChatDialog(),
			ProviderType.DeepSeek => new DeepSeekDialog(),
			_ => throw new NotImplementedException()
		};
	}
}
