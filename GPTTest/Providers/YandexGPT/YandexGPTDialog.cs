using System.Net.Http.Json;
using GPTProject.Common;


namespace GPTProject.Core.Providers.YandexGPT
{
	public class YandexGPTDialog : IChatDialog
	{
		private List<YandexMessage> messagesHistory;
		private HttpClient httpClient;
		private const int minimalContentLength = 1;

		public YandexGPTDialog()
		{
			messagesHistory = new List<YandexMessage>();
			httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {Settings.apiKey}");
		}

		public async Task<string> SendMessage(string message)
		{
			if (message.Length < minimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			messagesHistory.Add(new YandexMessage()
			{
				Role = Role.User,
				Text = message
			});

			var prompt = new YandexRequest()
			{
				ModelUri = $"gpt://{Settings.CatalogId}/yandexgpt-lite",
				Messages = messagesHistory
			};

			using var response = await httpClient.PostAsJsonAsync(Settings.completionsEndpoint, prompt);

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

            messagesHistory.Add(new YandexMessage()
            {
                Role = Role.Assistant,
                Text = choice
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
				messagesHistory.RemoveRange(1, messagesHistory.Count-1);
			}
		}

		public void SetSystemPrompt(string message)
		{
			ClearDialog();
			if (string.IsNullOrEmpty(message))
			{
				throw new ArgumentException("message is null");
			}

			messagesHistory.Add(new YandexMessage() { Role = Role.System, Text = message });
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
				messagesHistory[0].Text = message;
			}
			else
			{
				ClearDialog();
				SetSystemPrompt(message);
			}
		}
	}
}
