using System.Text.Json.Serialization;

namespace GPTProject.Core.Models.Common
{
    public class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
