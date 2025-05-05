using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;
using System.Text.Json;
using QdrantExpansion.Models;
using QdrantExpansion.Services;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class RememberingAgent
	{
		private readonly IChatDialog _provider;
		private readonly DefaultQdrantService _qdrantService;
		private readonly JsonSerializerOptions _options;

		public RememberingAgent(IChatDialog provider, string collectionName, VectorizerRequest request) {
			_provider = provider;
			_qdrantService = new DefaultQdrantService(collectionName, request);
			_options = new JsonSerializerOptions
			{
				PropertyNameCaseInsensitive = true
			};
		}

		public void Update(List<IMessage> messagesHistory)
		{
			_provider.SetCustomDialog(messagesHistory);
			_provider.UpdateSystemPrompt(PromptManager.GetRemembererPrompt());
		}

		public async Task Run()
		{
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
			catch (Exception)
			{
				Console.WriteLine("Wrond JSON format");
			}
		}
	}
}
