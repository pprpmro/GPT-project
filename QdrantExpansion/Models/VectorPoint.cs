using System;
namespace QdrantExpansion.Models
{
	public class VectorPoint
	{
		public Guid Id { get; set; }
		public required float[] Vector { get; set; }
		public Dictionary<string, object?> Payload { get; set; } = [];
		public float Score { get; set; }
	}
}
