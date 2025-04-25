using System.Text.Json.Serialization;
using GPTProject.Core.Interfaces.Vectorizers;

namespace GPTProject.Core.Models.Common
{
	public class VectorizerRequest : IVectorizerRequest
	{
		[JsonIgnore]
		public string Key { get; set; }

		[JsonIgnore]
		public string Url { get; set; }

		[JsonPropertyName("input")]
		public string Input { get; set; } = "";

		[JsonPropertyName("model")]
		public string Model { get; set; } = "";

		[JsonPropertyName("encoding_format")] 
		public string Encoding_format { get; set; } = "";
	}
}
