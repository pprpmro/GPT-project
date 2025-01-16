using System.Text.Json.Serialization;

namespace GPTProject.Core.Providers.YandexGPT
{
    public class YandexRequest
    {
        [JsonPropertyName("modelUri")]
        public string ModelUri { get; set; } = "";

        [JsonPropertyName("completionOptions")]
        public Options Options { get; set; } = new Options();

        [JsonPropertyName("messages")]
        public List<YandexMessage> Messages { get; set; } = new();
    }

    public class Options
    {
        [JsonPropertyName("stream")]
        public bool isStream { get; set; } = false;
        [JsonPropertyName("maxTokens")]
        public int MaxTokens { get; set; } = 2048;
        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 0.7;
    }
}