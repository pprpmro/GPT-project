﻿using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using static GPTProject.Providers.Common.Configurations.YandexGPT;

namespace GPTProject.Providers.Vectorizers.Implementation
{
	public class YandexGPTVectorizer : IVectorizer
	{
		private string CurrentModel = EmbeddingModels.SmallTexts.Model;

		public async Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request)
		{
			var httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

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
