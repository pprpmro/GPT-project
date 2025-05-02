using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Implementation;
using GPTProject.Providers.Vectorizers.Interfaces;
using QdrantExpansion.Models;
using QdrantExpansion.Repository;

namespace QdrantExpansion.Services
{
	public class DefaultQdrantService
	{
		private readonly QdrantRepository _repository;
		private const int _defaultLimit = 5;
		private const float _defaultScoreThreshold = 0.7f;
		private IVectorizer _vectorizer;

		public string _collectionName;
		public VectorizerRequest _request;

		public DefaultQdrantService(string collectionName, VectorizerRequest request)
		{
			_repository = new QdrantRepository();
			_vectorizer = new DefaultVectorizer();
			_request = request;
			_collectionName = collectionName + "_" + _request.Model;
		}

		public async Task CreateCollectionAsync(int vectorSize)
		{
			try
			{
				await _repository.CreateCollectionAsync(_collectionName, vectorSize, DistanceType.Cosine);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public async Task DeleteCollectionAsync()
		{
			try
			{
				await _repository.DeleteCollectionAsync(_collectionName);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}

		public async Task UpsertStringsAsync(List<Payload> payloads)
		{
			try
			{
				var points = new List<VectorPoint>();
				var vectorizer = new DefaultVectorizer();

				var messages = new List<string>();

				foreach (var item in payloads)
				{
					messages.Add(item.Text);
				}

				_request.Input = messages.ToArray();

				var response = await vectorizer.GetEmbeddingAsync(_request);

				for (var i = 0; i < payloads.Count; i++) 
				{
					points.Add(
						new()
						{
							Id = Guid.NewGuid(),
							Vector = response.Embedding[i],
							Payload = payloads[i].GenerateDictionary()
						}
					);
				}

				await _repository.UpsertPointsAsync(_collectionName, points);
			}
			catch (Exception ex) 
			{
				Console.WriteLine(ex);
			}
		}

		public async Task<List<Dictionary<string, object?>>?> FindClosestAsync(string message, float scoreThreshold = _defaultScoreThreshold, int limit = _defaultLimit)
		{
			try
			{
				var payloads = new List<Dictionary<string, object?>>();

				_request.Input[0] = message;

				var response = await _vectorizer.GetEmbeddingAsync(_request);
				var result = await _repository.SearchAsync(_collectionName, response.Embedding[0], scoreThreshold, limit);

				foreach (var item in result) 
				{
					payloads.Add(item.Payload);
				}

				return payloads;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
				return null;
			}
		}
	}
}
