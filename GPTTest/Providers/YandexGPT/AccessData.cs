using System.Text.Json.Serialization;

namespace GPTProject.Core.Providers.YandexGPT
{
    public class AccessData
    {
        [JsonPropertyName("access_token")]
        public string AcessToken { get; set; } = string.Empty;
        [JsonPropertyName("expires_at")]
        public long? ExpiresAt { get; set; }
        [JsonPropertyName("code")]
        public int Code { get; set; }
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        public bool isExpired
        {
            get
            {
                if (!ExpiresAt.HasValue)
                {
                    return true;
                }
                else
                {
                    TimeSpan timeSpan = TimeSpan.FromMilliseconds(ExpiresAt.Value);
                    var expiresDataTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc) + timeSpan;
                    expiresDataTime = expiresDataTime.ToLocalTime();
                    return expiresDataTime < DateTime.Now;
                }
            }
        }
    }
}
