using System.Text.Json.Serialization;
using GPTProject.Core.Interfaces;

namespace GPTProject.Core.Providers.YandexGPT
{
    public class YandexMessage : IMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("text")]
        public string Content { get; set; } = "";
    }
}