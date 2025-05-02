using System.Text.Json.Serialization;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Providers.Data
{
	public class Request : IRequest
	{
		[JsonPropertyName("model")]
		public string Model { get; set; } = "";

		[JsonPropertyName("messages")]
		public List<IMessage> Messages { get; set; } = new();

		[JsonPropertyName("n")]
		public int AnswerCount { get; set; } = 1;

		[JsonPropertyName("temperature")]
		public double Temperature { get; set; } = 0.7;
	}
}
