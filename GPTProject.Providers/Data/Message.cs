using System.Text.Json.Serialization;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Providers.Data
{
	public class Message : IMessage
	{
		[JsonPropertyName("role")]
		public string Role { get; set; } = "";

		[JsonPropertyName("content")]
		public string Content { get; set; } = "";
	}
}
