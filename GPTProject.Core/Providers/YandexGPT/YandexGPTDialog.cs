using System.Net.Http.Json;
using GPTProject.Core.Models.Common;
using static GPTProject.Common.Config.Settings.YandexGPT;

namespace GPTProject.Core.Providers.YandexGPT
{
	public class YandexGPTDialog : BaseChatDialog<YandexMessage>
	{
		private const int minimalContentLength = 1;

		public YandexGPTDialog(int maxDialogHistorySize = 50)
		{
			messagesHistory = new List<YandexMessage>();
			httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {ApiKey}");

			MaxDialogHistorySize = maxDialogHistorySize;
			TotalSendedCharacterCount = 0;
		}

		public override async Task<string> SendMessage(string message, bool rememberMessage = true)
		{
			if (message.Length < minimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			messagesHistory.Add(new YandexMessage()
			{
				Role = DialogRole.User,
				Content = message
			});

			var prompt = new YandexRequest()
			{
				ModelUri = $"gpt://{CatalogId}/{Model}",
				Messages = messagesHistory
			};
			TotalSendedCharacterCount += GetHistoryCharacterCount();

			using var response = await httpClient.PostAsJsonAsync(CompletionsEndpoint, prompt);

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
				messagesHistory.Add(new YandexMessage()
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
				RaiseHistoryOverflowEvent();
				int removeCount = messagesHistory.Count - MaxDialogHistorySize;
				messagesHistory.RemoveRange(1, removeCount);
			}

			return choice;
		}
	}
}
