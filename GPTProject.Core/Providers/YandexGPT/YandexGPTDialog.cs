using System.Net.Http.Json;
using GPTProject.Core.Interfaces;
using GPTProject.Core.Models.Common;
using static GPTProject.Common.Config.Settings.YandexGPT;

namespace GPTProject.Core.Providers.YandexGPT
{
    public class YandexGPTDialog : IChatDialog
	{
		private List<YandexMessage> messagesHistory;
		private HttpClient httpClient;
		private const int minimalContentLength = 1;

		public int MaxDialogHistorySize { get; set; }

		public YandexGPTDialog(int maxDialogHistorySize = 50)
		{
			messagesHistory = new List<YandexMessage>();
			httpClient = new HttpClient();
			httpClient.DefaultRequestHeaders.Add("Authorization", $"Api-Key {ApiKey}");

			MaxDialogHistorySize = maxDialogHistorySize;
		}

		public async Task<string> SendMessage(string message, bool rememberMessage = true)
		{
			if (message.Length < minimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			messagesHistory.Add(new YandexMessage()
			{
				Role = DialogRole.User,
				Text = message
			});

			var prompt = new YandexRequest()
			{
				ModelUri = $"gpt://{CatalogId}/{Model}",
				Messages = messagesHistory
			};

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
					Text = choice
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
				messagesHistory.RemoveRange(1, messagesHistory.Count-1);
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

			bool hasSystemPrompt = messagesHistory[0].Role == DialogRole.Developer;

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

			if (messagesHistory.Count > 0 && messagesHistory[0].Role == DialogRole.Developer)
			{
				messagesHistory[0].Text = message;
			}
			else
			{
				messagesHistory.Insert(0, new YandexMessage() { Role = DialogRole.System, Text = message });
			}
		}
	}
}
