using System.Net.Http.Json;
using System.Text.Json;
using GPTProject.Common;

namespace GPTProject.Core.Providers.GigaChat
{
	public class GigaChatDialog : IChatDialog
	{
		private List<Message> messagesHistory;
		private HttpClient httpClient;
		private AccessData? accessData;
		private const int minimalContentLength = 1;
		private Guid RqUID;


		public GigaChatDialog()
		{
			messagesHistory = new List<Message>();
			httpClient = new HttpClient();
			RqUID = Guid.NewGuid();
		}

		private async Task<AccessData> GetAccessData()
		{
			httpClient.DefaultRequestHeaders.Clear();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Settings.authorizeData}");
			httpClient.DefaultRequestHeaders.Add("RqUID", RqUID.ToString());

			var scopeList = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("scope", Settings.scope) };
			using var response = await httpClient.PostAsync(Settings.accessTokenEndpoint, new FormUrlEncodedContent(scopeList));

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

		public async Task<string> SendMessage(string content)
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
				Model = "GigaChat:latest",
				Messages = messagesHistory
			};

			using var response = await httpClient.PostAsJsonAsync(Settings.completionsEndpoint, Request);

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

			messagesHistory.Add(new Message()
			{
				Role = Role.Assistant,
				Content = choice
			});

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

		public void SetSystemPrompt(string message)
		{

			 ClearDialog();
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentException("message is null");
			}

			messagesHistory.Add(new Message() { Role = Role.System, Content = message });
		}

		public void ReplaceSystemPrompt(string message, bool clearDialog = true)
		{
			if (clearDialog)
			{
				ClearDialog();
				SetSystemPrompt(message);
				return;
			}

			if (messagesHistory[0].Role == Role.System)
			{
				messagesHistory[0].Content = message;
			}
			else
			{
				ClearDialog();
				SetSystemPrompt(message);
			}
		}
	}
}
