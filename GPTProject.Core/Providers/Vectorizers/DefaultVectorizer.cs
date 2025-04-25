using GPTProject.Core.Models.Common;
using System.Net.Http.Json;
using System.Text.Json;

namespace GPTProject.Core.Providers.Vectorizers
{
	public class DefaultVectorizer : IVectorizer
	{
		public async Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {request.Key}");

			using var response = await httpClient.PostAsJsonAsync(request.Url, request);

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
			}

			using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
			var embedding = doc.RootElement
				.GetProperty("data")
				.EnumerateArray()
				.First()
				.GetProperty("embedding")
				.Deserialize<float[]>();

			return new VectorizerResponse { Embedding = embedding };
		}
	}
}
