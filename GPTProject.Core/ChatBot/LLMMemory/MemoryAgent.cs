using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;
using System.Text.Json;
using QdrantExpansion.Models;
using QdrantExpansion.Services;
using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Data.Dialogs;
using GPTProject.Providers.Vectorizers.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class MemoryAgent
	{
		private readonly IChatDialog _providerSaving;
		private readonly IChatDialog _providerRestoring;
		private readonly DefaultQdrantService _qdrantService;
		private readonly JsonSerializerOptions _options;

		public MemoryAgent(Dictionary<DialogType, ProviderType> providerTypes, string collectionName, VectorizerRequest request, IVectorizer vectorizer) {
			_providerSaving = DialogSelector.GetDialog(providerTypes, DialogType.Saving);
			_providerRestoring = DialogSelector.GetDialog(providerTypes, DialogType.Restoring);
			_qdrantService = new DefaultQdrantService(collectionName, request, vectorizer);
			_options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		public async Task Save(List<IMessage> messagesHistory)
		{
			_providerSaving.SetCustomDialog(messagesHistory);
			_providerSaving.UpdateSystemPrompt(PromptManager.GetSavingPrompt());

			var response = await _providerSaving.SendMessage(null, null, false);
			var responseJson = JsonUtil.ExtractJsonFromString(response);

			if (responseJson is null) { return; }
			try
			{
				List<Payload> payloads = JsonSerializer.Deserialize<List<Payload>>(responseJson, _options)
					.Select(p => new Payload
						{
							Text = p.Text,
							Importance = p.Importance
						}).ToList();
				await _qdrantService.CreateIfNeededAsync(1536); //изменить, вытягивать размер из списка векторизаторов
				await _qdrantService.UpsertStringsAsync(payloads);
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error", ex.Message);
			}
		}

		public async Task<string> Restore(string message)
		{
			_providerRestoring.UpdateSystemPrompt(PromptManager.GetRestoringPrompt());

			var response = await _providerRestoring.SendMessage(message);
			var responseJson = JsonUtil.ExtractJsonFromString(response);

			if (responseJson is null) { return string.Empty; }
			try
			{
				string[] strings = JsonSerializer.Deserialize<string[]>(responseJson);
				var searchResult = await _qdrantService.FindClosestForManyAsync(strings, 0.4f);

				if (searchResult is null)
				{
					throw new Exception("Database error");
				}
				var results = new List<string>();
				foreach (var item in searchResult)
				{
					if(!results.Contains(item["text"].ToString())) { results.Add(item["text"].ToString()); }
				}
				return string.Join(Environment.NewLine, results.ToArray());
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex.Message);
			}

			return string.Empty;
		}
	}
}
