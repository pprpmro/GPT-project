using System.Text.Json.Serialization;

namespace GPTProject.Core.Common
{
    public class Request
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();

        [JsonPropertyName("n")]
        public int AnswerCount { get; set; } = 1;

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }
}
