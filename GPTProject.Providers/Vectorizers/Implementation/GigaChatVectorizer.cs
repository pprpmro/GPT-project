using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using static GPTProject.Providers.Common.Configurations.GigaChat;

namespace GPTProject.Providers.Vectorizers.Implementation
{
	public class GigaChatVectorizer : IVectorizer
	{
		private string CurrentModel = EmbeddingModels.Default.Model;
		private HttpClient httpClient;
		private Guid RqUID;
		private GigaChatAccessData? accessData;

		public GigaChatVectorizer() 
		{
			httpClient = new HttpClient();
			RqUID = Guid.NewGuid();
		}
		public async Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request)
		{
			if (accessData == null || accessData.isExpired)
			{
				var newAccessData = await GetAccessData();
				accessData = newAccessData;
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

		private async Task<GigaChatAccessData> GetAccessData()
		{
			httpClient.DefaultRequestHeaders.Clear();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {AuthorizeData}");
			httpClient.DefaultRequestHeaders.Add("RqUID", RqUID.ToString());

			var scopeList = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("scope", Scope) };
			using var response = await httpClient.PostAsync(AccessTokenEndpoint, new FormUrlEncodedContent(scopeList));

			var accessData = GetAccessData(response);

			if (accessData == null)
			{
				throw new NullReferenceException(nameof(accessData));
			}

			httpClient.DefaultRequestHeaders.Clear();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessData.AccessToken}");
			return accessData;

			GigaChatAccessData? GetAccessData(HttpResponseMessage? response)
			{
				if (response == null)
				{
					throw new NullReferenceException(nameof(response));
				}

				string result = response.Content.ReadAsStringAsync().Result;
				if (response.IsSuccessStatusCode)
				{
					return JsonSerializer.Deserialize<GigaChatAccessData>(result);
				}
				else
				{
					throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
				}
			}
		}
	}
}
