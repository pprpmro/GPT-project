using System.Text.Json.Serialization;
using GPTProject.Providers.Vectorizers.Interfaces;

namespace GPTProject.Providers.Data.Vectorizers
{
	public class VectorizerResponse : IVectorizerResponse
	{
		[JsonPropertyName("embedding")]

		public float[][] Embedding { get; set; } = Array.Empty<float[]>();
	}
}
