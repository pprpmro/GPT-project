using GPTProject.Providers.Dialogs.Enumerations;
using System.Text.Json.Serialization;

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
				public static Embedder Small = new()
				{
					Model = "text-embedding-3-small",
					Lenght = 1536,
					Provider = ProviderType.ChatGPT
				};

				public static Embedder Large = new()
				{
					Model = "text-embedding-3-large",
					Lenght = 3072,
					Provider = ProviderType.ChatGPT
				};

				public static Embedder Ada = new()
				{
					Model = "text-embedding-ada-002",
					Lenght = 1536,
					Provider = ProviderType.ChatGPT
				};
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
				public static Embedder BigTexts = new()
				{
					Model = $"emb://{CatalogId}/text-search-doc/latest",
					Lenght = 256,
					Provider = ProviderType.YandexGPT
				};

				public static Embedder SmallTexts = new()
				{
					Model = $"$emb://{CatalogId}/text-search-query/latest",
					Lenght = 256,
					Provider = ProviderType.YandexGPT
				};
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
				public static Embedder Default = new()
				{
					Model = "Embeddings",
					Lenght = 512,
					Provider = ProviderType.GigaChat
				};

				public static Embedder Giga = new()
				{
					Model = "EmbeddingsGigaR",
					Lenght = 4096,
					Provider = ProviderType.GigaChat
				};
			}

			public class GigaChatAccessData
			{
				[JsonPropertyName("access_token")]
				public string AccessToken { get; set; } = string.Empty;
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
			public required string Model { get; set; }
			public required int Lenght { get; set; }
			public ProviderType Provider { get; set; }
		}

	}
}
