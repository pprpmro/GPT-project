using System.Text.Json.Serialization;

namespace GPTProject.Common
{
	public class Message
	{
		[JsonPropertyName("role")]
		public string Role { get; set; } = "";
		[JsonPropertyName("content")]
		public string Content { get; set; } = "";
	}
}
