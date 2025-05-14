using System.Text.Json.Serialization;

namespace GPTProject.Providers.Data.Dialogs
{
	public interface IRequest
	{
		int AnswerCount { get; set; }
		List<IMessage> Messages { get; set; }
		string Model { get; set; }
		double Temperature { get; set; }
		bool Stream {  get; set; }
	}

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

		[JsonPropertyName("stream")]
		public bool Stream { get; set; } = false;
	}
}