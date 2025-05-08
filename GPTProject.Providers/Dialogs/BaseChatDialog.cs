using System.Net.Http.Json;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Data;

namespace GPTProject.Providers.Dialogs
{
	public abstract class BaseChatDialog<TMessage, TRequest> : IChatDialog
		where TMessage : IMessage, new()
		where TRequest : IRequest, new()
	{
		protected List<IMessage> messagesHistory;
		protected HttpClient httpClient;
		protected const int MinimalContentLength = 1;

		protected readonly string modelName;
		protected readonly string completionsEndpoint;

		public delegate void HistoryOverflowEvent(List<IMessage> messagesHistory);
		public event HistoryOverflowEvent? HistoryOverflowNotify;

		protected BaseChatDialog(string modelName, string completionsEndpoint)
		{
			messagesHistory = new List<IMessage>();
			httpClient = new HttpClient();
			this.modelName = modelName;
			this.completionsEndpoint = completionsEndpoint;
			this.TotalSendedCharacterCount = 0;
		}

		public int MaxDialogHistorySize { get; set; }
		public int TotalSendedCharacterCount { get; set; }

		public int CurrentHistorySymbolsCount { get { return messagesHistory.Where(x => x.Content != null).Select(x => x.Content.Length).Sum(); } }
		public int MessageCount { get { return messagesHistory.Count; } }

		public void SetOverflowHandler(Action<List<IMessage>> handler)
		{
			HistoryOverflowNotify += new HistoryOverflowEvent(handler);
		}
		public virtual void ClearDialog(bool clearSystemPrompt = true)
		{
			if (clearSystemPrompt)
			{
				messagesHistory.Clear();
			}
			else if (messagesHistory.Count > 1)
			{
				messagesHistory.RemoveRange(1, messagesHistory.Count - 1);
			}
		}

		public virtual void ClearDialog(bool clearSystemPrompt = false, int? lastNMessages = null)
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

		public virtual void UpdateSystemPrompt(string message, bool clearDialog = false)
		{
			if (string.IsNullOrEmpty(message))
				throw new ArgumentException("System prompt cannot be null or empty.");

			if (clearDialog)
				messagesHistory.Clear();

			if (messagesHistory.Count > 0 && messagesHistory[0].Role == DialogRole.Developer)
			{
				messagesHistory[0].Content = message;
			}
			else
			{
				messagesHistory.Insert(0, new TMessage { Role = DialogRole.Developer, Content = message });
			}
		}

		public virtual async Task<string> SendMessage(string message, bool rememberMessage = true)
		{
			if (message.Length < MinimalContentLength)
			{
				throw new ArgumentException("Message length is less than minimum");
			}

			messagesHistory.Add(new TMessage()
			{
				Role = DialogRole.User,
				Content = message
			});

			var requestData = new TRequest()
			{
				Model = modelName,
				Messages = messagesHistory
			};
			TotalSendedCharacterCount += GetHistoryCharacterCount();

			using var response = await httpClient.PostAsJsonAsync(completionsEndpoint, requestData);

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
				HistoryOverflowNotify?.Invoke(messagesHistory);
				int removeCount = messagesHistory.Count - MaxDialogHistorySize;
				messagesHistory.RemoveRange(1, removeCount);
			}

			return choice;
		}

		public async Task<string> SendMessage()
		{
			var requestData = new TRequest()
			{
				Model = modelName,
				Messages = messagesHistory
			};

			using var response = await httpClient.PostAsJsonAsync(completionsEndpoint, requestData);

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
			return choice;
		}

		public int GetHistoryCharacterCount()
		{
			return messagesHistory.Sum(m => m.Content.Length);
		}

		public void SetCustomDialog(List<IMessage> customMessagesHistory)
		{
			messagesHistory = customMessagesHistory;
		}

		public List<IMessage> GetDialog()
		{
			return messagesHistory;
		}
	}
}
