using System.Net.Http.Json;
using System.Text.Json;
using GPTProject.Common;
using static GPTProject.Core.Providers.Settings.GigaChat;

namespace GPTProject.Core.Providers.GigaChat
{
	public class GigaChatDialog : IChatDialog
	{
		private List<Message> messagesHistory;
		private HttpClient httpClient;
		private AccessData? accessData;
		private const int minimalContentLength = 1;
		private Guid RqUID;

		public int MaxDialogHistorySize { get; set; }

		public GigaChatDialog(int maxDialogHistorySize = 50)
		{
			messagesHistory = new List<Message>();
			httpClient = new HttpClient();
			RqUID = Guid.NewGuid();

			MaxDialogHistorySize = maxDialogHistorySize;
		}

		private async Task<AccessData> GetAccessData()
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

			AccessData? GetAccessData(HttpResponseMessage? response)
			{
				if (response == null)
				{
					throw new NullReferenceException(nameof(response));
				}

				string result = response.Content.ReadAsStringAsync().Result;
				if (response.IsSuccessStatusCode)
				{
					return JsonSerializer.Deserialize<AccessData>(result);
				}
				else
				{
					throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
				}
			}
		}

		public async Task<string> SendMessage(string content, bool rememberMessage = true)
		{
			if (content.Length < minimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			if (accessData == null || accessData.isExpired)
			{
				var newAccessData = await GetAccessData();
				accessData = newAccessData;
			}

			messagesHistory.Add(new Message()
			{
				Role = Role.User,
				Content = content
			});

			var Request = new Request()
			{
				Model = Model,
				Messages = messagesHistory
			};

			using var response = await httpClient.PostAsJsonAsync(CompletionsEndpoint, Request);

			if (!response.IsSuccessStatusCode)
			{
				throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
			}

			ResponseData? responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
			var choices = responseData?.Choices ?? new List<Choice>();
			if (choices.Count == 0)
			{
				throw new Exception("No choices were returned by the API");
			}

			var choice = choices[0].Message.Content.Trim();

			if (rememberMessage)
			{
				messagesHistory.Add(new Message()
				{
					Role = Role.Assistant,
					Content = choice
				});
			}
			else
			{
				messagesHistory.RemoveAt(messagesHistory.Count - 1);
			}

			if (messagesHistory.Count > MaxDialogHistorySize)
			{
				int removeCount = messagesHistory.Count - MaxDialogHistorySize;
				messagesHistory.RemoveRange(1, removeCount);
			}

			return choice;
		}
		public void ClearDialog(bool clearSystemPrompt = true)
		{
			if (clearSystemPrompt)
			{
				messagesHistory.Clear();
			}
			else
			{
				messagesHistory.RemoveRange(1, messagesHistory.Count - 1);
			}
		}

		public void ClearDialog(bool clearSystemPrompt = false, int? lastNMessages = null)
		{
			if (clearSystemPrompt)
			{
				messagesHistory.Clear();
				return;
			}

			if (messagesHistory.Count == 0) return;

			bool hasSystemPrompt = messagesHistory[0].Role == Role.Developer;

			if (lastNMessages.HasValue)
			{
				int removeCount = Math.Min(lastNMessages.Value, messagesHistory.Count - (hasSystemPrompt ? 1 : 0));

				if (removeCount > 0)
				{
					messagesHistory.RemoveRange(messagesHistory.Count - removeCount, removeCount);
				}
			}
			else
			{
				messagesHistory.RemoveRange(hasSystemPrompt ? 1 : 0, messagesHistory.Count - (hasSystemPrompt ? 1 : 0));
			}
		}

		public void UpdateSystemPrompt(string message, bool clearDialog = false)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("System prompt cannot be null or empty.");

			if (clearDialog)
				messagesHistory.Clear();

			if (messagesHistory.Count > 0 && messagesHistory[0].Role == Role.Developer)
			{
				messagesHistory[0].Content = message;
			}
			else
			{
				messagesHistory.Insert(0, new Message { Role = Role.Developer, Content = message });
			}
		}
	}
}
