using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Data.Dialogs;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class PlanningAgent
	{
		private readonly IChatDialog _provider;

		public PlanningAgent(Dictionary<DialogType, ProviderType> providerTypes) {
			_provider = DialogUtil.GetDialog(providerTypes, DialogType.Saving);
		}

		public async Task<string> MakeNewPlan(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(DialogUtil.CleanMessagesHistoryFromPrompts(messagesHistory));
			_provider.UpdateSystemPrompt(PromptManager.GetMakePlanPrompt());

			 return await _provider.SendMessage(null, null, false);
		}

		public async Task<string> RemakeOldPlan(List<IMessage> messagesHistory, string oldPlan)
		{
			_provider.SetCustomDialog(DialogUtil.CleanMessagesHistoryFromPrompts(messagesHistory));
			_provider.UpdateSystemPrompt(PromptManager.GetRemakePlanPrompt(oldPlan));

			var a = await _provider.SendMessage(null, null, false);
			return a;
		}
	}
}
