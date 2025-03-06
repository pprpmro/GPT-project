using System.Text.Json.Serialization;

namespace GPTProject.Core.Models
{
    public class SmallTalkResponse
    {
        [JsonPropertyName("SMALL_TALK")]
        public string SmallTalk { get; set; } = "EMPTY";

        [JsonPropertyName("QUESTIONS")]
        public string Questions { get; set; } = "EMPTY";
    }
}