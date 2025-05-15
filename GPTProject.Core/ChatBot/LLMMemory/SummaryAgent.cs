using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Data.Dialogs;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class SummaryAgent
	{
		private readonly IChatDialog _provider;

		public SummaryAgent(Dictionary<DialogType, ProviderType> providerTypes, string collectionName, VectorizerRequest request) {
			_provider = DialogSelector.GetDialog(providerTypes, DialogType.Summary);
		}

		public async Task<string> GetSummaryForContext(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetSummaryContentPrompt());

			return await _provider.SendMessage(null, null, false);
		}

		public async Task<string> GetSummaryForSaving(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetSummaryForSavingPrompt());

			return await _provider.SendMessage(null, null, false);
		}
	}
}
