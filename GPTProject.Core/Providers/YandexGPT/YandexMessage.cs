using System.Text.Json.Serialization;

namespace GPTProject.Core.Providers.YandexGPT
{
    public class YandexMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("text")]
        public string Text { get; set; } = "";
    }
}