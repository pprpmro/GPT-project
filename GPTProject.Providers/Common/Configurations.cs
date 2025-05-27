using GPTProject.Providers.Dialogs.Enumerations;

namespace GPTProject.Providers.Common
{
	public static class Configurations
	{
		public static class ChatGPT
		{
			public static string ApiKey => EnvironmentLoader.GetEnvironmentVariable("CHATGPT_API_KEY");

			public static string DialogCompletionsEndpoint = "https://api.openai.com/v1/chat/completions";
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

			public static string EmbeddingEndpoint = "https://api.openai.com/v1/embeddings";
			public static class EmbeddingModels
			{
				public static Embedder Small = new("text-embedding-3-small", 1536, ProviderType.ChatGPT);

				public static Embedder Large = new("text-embedding-3-large", 3072, ProviderType.ChatGPT);

				public static Embedder Ada = new("text-embedding-ada-002", 1536, ProviderType.ChatGPT);
			}
		}

		public static class YandexGPT
		{
			public static string ApiKey => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_API_KEY");
			public static string CatalogId => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_CATALOG_ID");
			public static string KeyId => EnvironmentLoader.GetEnvironmentVariable("YANDEXGPT_KEY_ID");

			public static string DialogCompletionsEndpoint = "https://llm.api.cloud.yandex.net/foundationModels/v1/completion";
			public static string OpenAILikeDialogCompletionsEndpoint = "https://llm.api.cloud.yandex.net/v1/chat/completions";

			public static class DialogModels
			{
				public static string Lite = $"gpt://{CatalogId}/yandexgpt-lite/latest";
				public static string Pro = $"gpt://{CatalogId}/yandexgpt/latest";
			}

			public static string EmbeddingEndpoint = "https://llm.api.cloud.yandex.net/foundationModels/v1/tokenize";
			public static class EmbeddingModels
			{
				public static Embedder BigTexts = new($"emb://{CatalogId}/text-search-doc/latest", 256, ProviderType.YandexGPT);

				public static Embedder SmallTexts = new($"$emb://{CatalogId}/text-search-query/latest", 256, ProviderType.YandexGPT);
			}
		}

		public static class GigaChat
		{
			public static string AuthorizeData => EnvironmentLoader.GetEnvironmentVariable("GIGACHAT_AUTHORIZE_DATA");

			public static string Scope = "GIGACHAT_API_PERS";
			public static string AccessTokenEndpoint = "https://ngw.devices.sberbank.ru:9443/api/v2/oauth";

			public static string DialogEndpoint = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
			public static class DialogModels
			{
				public static string Pro = "GigaChat-Pro";
				public static string Pro_preview = "GigaChat-Pro-preview";
				public static string Pro2 = "GigaChat-2-Pro";

				public static string Max = "GigaChat-Max";
				public static string Max_preview = "GigaChat-Max-preview";
				public static string Max2 = "GigaChat-2-Max";

				public static string Lite = "GigaChat";
				public static string Lite_preview = "GigaChat-preview";
				public static string Lite2 = "GigaChat-2";
			}

			public static string EmbeddingEndpoint = "https://gigachat.devices.sberbank.ru/api/v1/embeddings";
			public static class EmbeddingModels
			{
				public static Embedder Default = new("Embeddings", 512, ProviderType.GigaChat);

				public static Embedder Giga = new("EmbeddingsGigaR", 4096, ProviderType.GigaChat);
			}
		}

		public static class DeepSeek
		{
			public static string ApiKey => EnvironmentLoader.GetEnvironmentVariable("DEEPSEEK_API_KEY");

			public static string DialogCompletionsEndpoint = "https://api.deepseek.com/chat/completions";
			public static class DialogModels
			{
				public static string Default = "deepseek-chat";
				public static string Reasoner = "deepseek-reasoner";
			}
		}

		public class Embedder
		{
			public string Model { get; }
			public int Lenght { get; }
			public ProviderType Provider { get; }

			public Embedder(string model, int lenght, ProviderType provider)
			{
				Model = model;
				Lenght = lenght;
				Provider = provider;
			}
		}

	}
}
