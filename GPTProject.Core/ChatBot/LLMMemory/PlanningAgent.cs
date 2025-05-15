using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;
using System.Text.Json;
using QdrantExpansion.Models;
using QdrantExpansion.Services;
using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Data.Dialogs;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class PlanningAgent
	{
		private readonly IChatDialog _provider;
		private readonly DefaultQdrantService _qdrantService;
		private readonly JsonSerializerOptions _options;

		public PlanningAgent(Dictionary<DialogType, ProviderType> providerTypes, string collectionName, VectorizerRequest request) {
			_provider = DialogSelector.GetDialog(providerTypes, DialogType.Saving);
			_qdrantService = new DefaultQdrantService(collectionName, request);
			_options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		public async Task<string> MakeNewPlan(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetMakePlanPrompt());

			 return await _provider.SendMessage(null, null, false);
		}

		public async Task<string> RemakeOldPlan(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetRemakePlanPrompt());

			return await _provider.SendMessage(null, null, false);
		}
	}
}
