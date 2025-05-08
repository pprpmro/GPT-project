using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;
using System.Text.Json;
using QdrantExpansion.Models;
using QdrantExpansion.Services;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class MemoryAgent
	{
		private readonly IChatDialog _provider;
		private readonly DefaultQdrantService _qdrantService;
		private readonly JsonSerializerOptions _options;

		public MemoryAgent(IChatDialog provider, string collectionName, VectorizerRequest request) {
			_provider = provider;
			_qdrantService = new DefaultQdrantService(collectionName, request);
			_options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		public async Task Save(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetSavingPrompt());

			var response = await _provider.SendMessage();
			try
			{
				List<Payload> payloads = JsonSerializer.Deserialize<List<Payload>>(response, _options)
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
			_provider.UpdateSystemPrompt(PromptManager.GetRestoringPrompt());

			var response = await _provider.SendMessage(message);
			try
			{
				string[] strings = JsonSerializer.Deserialize<string[]>(response);
				var searchResult = await _qdrantService.FindClosestForManyAsync(strings, 0.4f);

				var results = new List<string>();
				foreach (var item in searchResult)
				{
					if(!results.Contains(item["text"].ToString())) { results.Add(item["text"].ToString()); }
				}
				return string.Join(Environment.NewLine, results.ToArray());
			}
			catch (Exception ex)
			{
				Console.WriteLine("Error", ex.Message);
			}

			return string.Empty;
		}
	}
}
