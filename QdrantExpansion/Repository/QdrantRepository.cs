using Google.Protobuf.Collections;
using Qdrant.Client;
using Qdrant.Client.Grpc;
using QdrantExpansion.Models;
using System.Text.Json;

namespace QdrantExpansion.Repository
{
	public class QdrantRepository : IVectorRepository
	{
		private readonly QdrantClient _client;

		public QdrantRepository(string host = "localhost", int port = 6334)
		{
			_client = new QdrantClient(host, port);
		}

		private readonly JsonSerializerOptions _jsonOptions = new()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase
		};

		public async Task CreateCollectionAsync(string collectionName, int vectorSize, DistanceType distanceType)
		{
			await _client.CreateCollectionAsync(
				collectionName: collectionName,
				vectorsConfig: new VectorParams
				{
					Size = (ulong)vectorSize,
					Distance = ConvertDistanceType(distanceType)
				});
		}

		public async Task UpsertPointsAsync(string collectionName, IEnumerable<VectorPoint> points)
		{
			var qdrantPoints = new List<PointStruct>();

			foreach (var point in points)
			{
				var pointStruct = new PointStruct
				{
					Id = point.Id,
					Vectors = point.Vector
				};

				var payload = new MapField<string, Value>();
				foreach (var kvp in point.Payload)
				{
					payload[kvp.Key] = JsonSerializer.Serialize(kvp.Value, _jsonOptions);
				}

				pointStruct.Payload.Add(payload);
				qdrantPoints.Add(pointStruct);
			}

			await _client.UpsertAsync(
				collectionName: collectionName,
				points: qdrantPoints);
		}

		public async Task<IEnumerable<VectorPoint>> SearchAsync(
		string collectionName,
		float[] vector,
		float scoreThreshold,
		int limit)
		{
			var results = await _client.SearchAsync(
				collectionName: collectionName,
				vector: vector,
				scoreThreshold: scoreThreshold,
				limit: (ulong)limit,
				payloadSelector: new WithPayloadSelector { Enable = true },
				vectorsSelector: new WithVectorsSelector { Enable = true }
			);

			var points = new List<VectorPoint>();

			foreach (var result in results)
			{

				var payload = result.Payload.ToDictionary(
					kvp => kvp.Key,
					kvp => JsonSerializer.Deserialize<object>(kvp.Value.StringValue, _jsonOptions));

				points.Add(new VectorPoint
				{
					Id = Guid.Parse(result.Id.Uuid),
					Vector = [.. result.Vectors.Vector.Data],
					Payload = payload,
					Score = result.Score
				});
			}

			return points;
		}

		public async Task<IEnumerable<VectorPoint>> SearchBatchAsync(
		string collectionName,
		float[][] vectors,
		float scoreThreshold,
		int limit)
		{
			var pointsBatch = new List<SearchPoints>();

			foreach (var vector in vectors)
			{
				pointsBatch.Add(
					new SearchPoints
					{
						Vector = { vector },
						Limit = (ulong)limit,
						WithPayload = true,
						WithVectors = true,
						ScoreThreshold = scoreThreshold
					}
				);
			}

			var batchResults = await _client.SearchBatchAsync(collectionName, pointsBatch);

			var points = new List<VectorPoint>();

			foreach (var batchResult in batchResults)
			{
				foreach (var result in batchResult.Result)
				{
					var payload = result.Payload.ToDictionary(
					kvp => kvp.Key,
					kvp => JsonSerializer.Deserialize<object>(kvp.Value.StringValue, _jsonOptions));

					points.Add(new VectorPoint
					{
						Id = Guid.Parse(result.Id.Uuid),
						Vector = [.. result.Vectors.Vector.Data],
						Payload = payload,
						Score = result.Score
					});
				}
			}

			return points;
		}

		public async Task DeletePayloadAsync(string collectionName, string payloadKey, ulong pointId)
		{
			await _client.DeletePayloadAsync(
				collectionName: collectionName,
				keys: [payloadKey],
				id: pointId);
		}

		public async Task DeletePointAsync(string collectionName, ulong pointId)
		{
			await _client.DeleteAsync(
				collectionName: collectionName,
				id: pointId);
		}

		public async Task DeleteCollectionAsync(string collectionName)
		{
			await _client.DeleteCollectionAsync(collectionName);
		}

		public async Task<List<QdrantCollectionInfo>> GetAllCollectionsInfoAsync()
		{
			var collections = await _client.ListCollectionsAsync();
			var result = new List<QdrantCollectionInfo>();

			foreach (var name in collections)
			{
				var info = await _client.GetCollectionInfoAsync(name);
				result.Add(new QdrantCollectionInfo
				{
					Name = name,
					VectorSize = info.Config.Params.VectorsConfig.Params.Size,
					DistanceType = info.Config.Params.VectorsConfig.Params.Distance,
					PointsCount = info.PointsCount
				});
			}

			return result;
		}

		private static Distance ConvertDistanceType(DistanceType type)
		{
			return type switch
			{
				DistanceType.Cosine => Distance.Cosine,
				DistanceType.Euclid => Distance.Euclid,
				DistanceType.Dot => Distance.Dot,
				_ => Distance.UnknownDistance
			};
		}
	}
}
