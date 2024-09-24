using System.Text.Json.Serialization;

namespace GPTTest.Providers.GigaChat.Common
{
    class Request
    {
        [JsonPropertyName("model")]
        public string ModelId { get; set; } = "GigaChat:latest";
        [JsonPropertyName("messages")]
        public List<Message> Messages { get; set; } = new();
        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 2048;
        [JsonPropertyName("n")]
        public int AnswerCount { get; set; } = 1;
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }
}
