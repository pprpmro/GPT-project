using System.Text.Json.Serialization;

namespace GPTProject.Common
{
    public class Request
    {
        [JsonPropertyName("model")]
        public string ModelId { get; set; } = "";
        [JsonPropertyName("modelUri")]
        public string ModelUri { get; set; } = "";
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();
        public int MaxTokens { get; set; } = 2048;
        [JsonPropertyName("n")]
        public int AnswerCount { get; set; } = 1;
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }
}
