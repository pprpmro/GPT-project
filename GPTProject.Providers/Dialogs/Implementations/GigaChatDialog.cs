using System.Text.Json;
using System.Text.Json.Serialization;
using GPTProject.Providers.Data;
using static GPTProject.Providers.Common.Configurations.GigaChat;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class GigaChatDialog : BaseChatDialog<Message, Request>
	{
		private GigaChatAccessData? accessData;
		private const int minimalContentLength = 1;
		private Guid RqUID;

		public GigaChatDialog(int maxDialogHistorySize = 50)
			: base(DialogModels.Pro, DialogEndpoint)
		{
			RqUID = Guid.NewGuid();
			MaxDialogHistorySize = maxDialogHistorySize;
		}

		private async Task<GigaChatAccessData> GetAccessData()
		{
			httpClient.DefaultRequestHeaders.Clear();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {AuthorizeData}");
			httpClient.DefaultRequestHeaders.Add("RqUID", RqUID.ToString());

			var scopeList = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("scope", Scope) };
			using var response = await httpClient.PostAsync(AccessTokenEndpoint, new FormUrlEncodedContent(scopeList));

			var accessData = GetAccessData(response);

			if (accessData == null)
			{
				throw new NullReferenceException(nameof(accessData));
			}

			httpClient.DefaultRequestHeaders.Clear();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessData.AcessToken}");
			return accessData;

			GigaChatAccessData? GetAccessData(HttpResponseMessage? response)
			{
				if (response == null)
				{
					throw new NullReferenceException(nameof(response));
				}

				string result = response.Content.ReadAsStringAsync().Result;
				if (response.IsSuccessStatusCode)
				{
					return JsonSerializer.Deserialize<GigaChatAccessData>(result);
				}
				else
				{
					throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
				}
			}
		}

		public override async Task<string> SendMessage(string content, bool rememberMessage = true)
		{
			messagesHistory[0].Role = DialogRole.System;
			if (content.Length < minimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			if (accessData == null || accessData.isExpired)
			{
				var newAccessData = await GetAccessData();
				accessData = newAccessData;
			}

			return await base.SendMessage(content, rememberMessage);
		}

		private class GigaChatAccessData
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
}
