﻿using System.Text.Json.Serialization;

namespace GPTTest.Providers.GigaChat.Common
{
    class Message
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = "";
        [JsonPropertyName("content")]
        public string Content { get; set; } = "";
    }
}
