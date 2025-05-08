using QdrantExpansion.Models;
namespace QdrantExpansion.Repository
{
	public interface IVectorRepository
	{
		Task CreateCollectionAsync(string collectionName, int vectorSize, DistanceType distanceType);
		Task UpsertPointsAsync(string collectionName, IEnumerable<VectorPoint> points);
		Task<IEnumerable<VectorPoint>> SearchAsync(string collectionName, float[] vector, float scoreThreshold, int limit);
		Task<IEnumerable<VectorPoint>> SearchBatchAsync(string collectionName, float[][] vectors, float scoreThreshold, int limit);
		Task DeletePayloadAsync(string collectionName, string payloadKey, ulong pointId);
		Task DeletePointAsync(string collectionName, ulong pointId);
		Task DeleteCollectionAsync(string collectionName);
		Task<List<QdrantCollectionInfo>> GetAllCollectionsInfoAsync();
	}
}
