namespace GPTProject.Providers.Configurations
{
	public static class DialogConfigurations
	{
		public static class ChatGPT
		{
			public static string ApiKey => EnvironmentLoader.GetEnvironmentVariable("CHATGPT_API_KEY");

			public static string CompletionsEndpoint = "https://api.openai.com/v1/chat/completions";
			public static class DialogModels
			{
				public static string GPT_3_5_Turbo = "gpt-3.5-turbo";
				public static string GPT_4 = "gpt-4";
				public static string GPT_4_Turbo = "gpt-4-turbo";
				public static string GPT_4o = "gpt-4o";
				public static string GPT_4o_Mini = "gpt-4o-mini";
				public static string GPT_4_1 = "gpt-4.1";
				public static string GPT_4_1_Mini = "gpt-4.1-mini";
				public static string GPT_4_1_Nano = "gpt-4.1-nano";
				public static string GPT_4_5 = "gpt-4.5";
				public static string O1 = "o1";
				public static string O1_Mini = "o1-mini";
				public static string O3 = "o3";
				public static string O3_Mini = "o3-mini";
				public static string O4_Mini = "o4-mini";
			}
		}

		public static class YandexGPT
		{
			public static string ApiKey => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_API_KEY");
			public static string CatalogId => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_CATALOG_ID");
			public static string KeyId => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_KEY_ID");

			public static string CompletionsEndpoint = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";

			public static class DialogModels
			{
				public static string Lite = "yandexgpt-lite";
			}
		}

		public static class GigaChat
		{
			public static string AuthorizeData => EnvironmentLoader.GetEnvironmentVariable("GIGACHAT_AUTHORIZE_DATA");

			public static string Scope = "GIGACHAT_API_PERS";
			public static string AccessTokenEndpoint = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";
			public static string CompletionsEndpoint = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
			public static class DialogModels
			{
				public static string Pro = "GigaChat-Pro";
				public static string Default = "GigaChat:latest";
			}
		}
	}
}
