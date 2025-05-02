using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

namespace GPTProject.Providers.Vectorizers.Implementation
{
	public class DefaultVectorizer : IVectorizer
	{
		public async Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.Key}");

			var response = await httpClient.PostAsJsonAsync(request.Url, request);

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
	}
}
