namespace GPTProject.Providers.Config
{
	public static class Settings
	{
		static Settings()
		{
			EnvLoader.Load();
		}

		public static class ChatGPT
		{
			public static string ApiKey => GetRequiredEnv("CHATGPT_API_KEY");
			public static string CompletionsEndpoint => GetRequiredEnv("CHATGPT_COMPLETIONS_ENDPOINT");
			public static string Model => GetRequiredEnv("CHATGPT_MODEL");
		}

		public static class YandexGPT
		{
			public static string ApiKey => GetRequiredEnv("YANDEXGPT_API_KEY");
			public static string CompletionsEndpoint => GetRequiredEnv("YANDEXGPT_COMPLETIONS_ENDPOINT");
			public static string CatalogId => GetRequiredEnv("YANDEXGPT_CATALOG_ID");
			public static string KeyId => GetRequiredEnv("YANDEXGPT_KEY_ID");
			public static string Model => GetRequiredEnv("YANDEXGPT_MODEL");
		}

		public static class GigaChat
		{
			public static string AuthorizeData => GetRequiredEnv("GIGACHAT_AUTHORIZE_DATA");
			public static string Scope => GetRequiredEnv("GIGACHAT_SCOPE");
			public static string AccessTokenEndpoint => GetRequiredEnv("GIGACHAT_ACCESS_TOKEN_ENDPOINT");
			public static string CompletionsEndpoint => GetRequiredEnv("GIGACHAT_COMPLETIONS_ENDPOINT");
			public static string Model => GetRequiredEnv("GIGACHAT_MODEL");
		}

		private static string GetRequiredEnv(string variable)
		{
			return Environment.GetEnvironmentVariable(variable) ?? throw new Exception($"Переменная среды {variable} не найдена! Добавьте её в .env или в системные переменные.");
		}
	}


}
