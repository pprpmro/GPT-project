using System.Text.Json.Serialization;

namespace GPTProject.Providers.Data.Dialogs
{
	public interface IMessage
	{
		string Role { get; set; }

		string Content { get; set; }
	}

	public class Message : IMessage
	{
		[JsonPropertyName("role")]
		public string Role { get; set; } = "";

		[JsonPropertyName("content")]
		public string Content { get; set; } = "";
	}
}
