using Qdrant.Client.Grpc;

namespace QdrantExpansion.Models
{
	public class QdrantCollectionInfo
	{
		public string Name { get; set; }
		public ulong VectorSize { get; set; }
		public Distance DistanceType { get; set; }
		public ulong PointsCount { get; set; }
	}
}
