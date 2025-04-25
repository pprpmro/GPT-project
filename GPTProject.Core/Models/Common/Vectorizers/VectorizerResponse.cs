using System.Text.Json.Serialization;
using GPTProject.Core.Interfaces.Vectorizers;

namespace GPTProject.Core.Models.Common
{
	public class VectorizerResponse : IVectorizerResponse
	{
		[JsonPropertyName("embedding")]

		public float[] Embedding { get; set; } = Array.Empty<float>();
	}
}
