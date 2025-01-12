namespace GPTProject.Core.Providers.GigaChat
{
    public static class Settings
    {
        public static readonly string apiKey = "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";
        public static readonly string clientId = "10e377f8-5fc5-4288-a041-8c2bf7731911";
        public static readonly string clientSecret = "41bca7ca-6324-4d18-9eb5-ca73eb38e1c2";
        public static readonly string authorizeData = "MTBlMzc3ZjgtNWZjNS00Mjg4LWEwNDEtOGMyYmY3NzMxOTExOjQxYmNhN2NhLTYzMjQtNGQxOC05ZWI1LWNhNzNlYjM4ZTFjMg==";

        public static readonly string completionsEndpoint = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
        public static readonly string accessTokenEndpoint = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

        public static readonly string scope = "GIGACHAT_API_PERS";

        public static readonly string[] roleDescription = { "system", "user", "assistant" };
    }
}
