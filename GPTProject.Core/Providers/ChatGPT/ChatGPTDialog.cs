using System.Net.Http.Json;
using GPTProject.Core.Models.Common;
using static GPTProject.Common.Config.Settings.ChatGPT;

namespace GPTProject.Core.Providers.ChatGPT
{
	public class ChatGPTDialog : BaseChatDialog<Message>
	{
		public int MessageCount { get { return messagesHistory.Count; } }

		public ChatGPTDialog(int maxDialogHistorySize = 50)
		{
			messagesHistory = new List<Message>();
			httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");

			MaxDialogHistorySize = maxDialogHistorySize;

		}

		public override async Task<string> SendMessage(string message, bool rememberMessage = true)
		{
			if (message.Length < MinimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			messagesHistory.Add(new Message()
			{
				Role = DialogRole.User,
				Content = message
			});

			var requestData = new Request()
			{
				Model = Model,
				Messages = messagesHistory
			};

			using var response = await httpClient.PostAsJsonAsync(CompletionsEndpoint, requestData);

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
					Role = DialogRole.Assistant,
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
	}
}
