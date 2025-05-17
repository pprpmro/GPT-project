using System.Net.Http.Json;
using System.Text.Json.Nodes;
using GPTProject.Providers.Common;
using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;
using static GPTProject.Providers.Common.Configurations.GigaChat;

namespace GPTProject.Providers.Vectorizers.Implementation
{
	public class GigaChatVectorizer : IVectorizer
	{
		private string CurrentModel = EmbeddingModels.Default.Model;

		private readonly GigaChatAuthentificator authentificator;
		private readonly HttpClient httpClient;

		public GigaChatVectorizer() 
		{
			httpClient = new HttpClient();
			authentificator = new GigaChatAuthentificator(httpClient);
		}
		public async Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request)
		{
			var authenticated = await authentificator.EnsureAccessData();
			if (!authenticated)
			{
				throw new InvalidOperationException("Cant get access data");
			}

			request.Model = CurrentModel;

			var response = await httpClient.PostAsJsonAsync(EmbeddingEndpoint, request);

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
			}

			var node = JsonNode.Parse(await response.Content.ReadAsStringAsync())!;
			var embeddings = node["data"]!
				.AsArray()
				.Select(item => item!["embedding"]!
								.AsArray()
								.Select(x => (float)x!.GetValue<double>())
								.ToArray())
				.ToArray();

			return new VectorizerResponse { Embedding = embeddings };
		}

		public void SetModel(string model)
		{
			CurrentModel = model;
		}
	}
}
