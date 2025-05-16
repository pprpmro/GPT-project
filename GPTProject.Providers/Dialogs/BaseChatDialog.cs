using System.Data;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using GPTProject.Providers.Data.Dialogs;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Providers.Dialogs
{
	public abstract class BaseChatDialog<TMessage, TRequest> : IChatDialog
		where TMessage : IMessage, new()
		where TRequest : IRequest, new()
	{
		private readonly ITokenCalculator tokenCalculator = new TokenCalculator();

		protected List<IMessage> messagesHistory;
		protected HttpClient httpClient;

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
		}

		private int sessionTokenUsage;
		public int SessionTokenUsage { get { return sessionTokenUsage; } }


		public int MaxTokenHistorySize { get; set; } = 10000;
		public int CurrentTokenHistorySize { get { return RecalculateHistoryTokens(); } }
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

			if (messagesHistory.Count > 0 && (messagesHistory[0].Role == DialogRole.Developer || messagesHistory[0].Role == DialogRole.System))
			{
				messagesHistory[0].Content = message;
			}
			else
			{
				var role = this switch
				{
					ChatGPTDialog => DialogRole.Developer,
					_ => DialogRole.System
				};

				messagesHistory.Insert(0, new TMessage { Role = role, Content = message });
			}
		}

		public virtual async Task<string> SendMessage(
			string? message,
			Action<string>? onStreamedData = null,
			bool stream = false,
			bool rememberMessage = true)
		{
			if(!string.IsNullOrEmpty(message))
			{
				messagesHistory.Add(new TMessage()
				{
					Role = DialogRole.User,
					Content = message
				});
			}

			var requestData = new TRequest()
			{
				Model = modelName,
				Messages = messagesHistory,
				Stream = stream,
			};

			using var request = new HttpRequestMessage(HttpMethod.Post, completionsEndpoint)
			{
				Content = JsonContent.Create(requestData)
			};
			using var response = await httpClient.SendAsync(request, stream ? HttpCompletionOption.ResponseHeadersRead : HttpCompletionOption.ResponseContentRead);
			response.EnsureSuccessStatusCode();

			string assistantResponse = stream
				? await ReadStreamedResponse(response, onStreamedData)
				: await ReadDirectResponse(response);

			if (rememberMessage)
			{
				messagesHistory.Add(new Message()
				{
					Role = DialogRole.Assistant,
					Content = assistantResponse
				});
			}
			else
			{
				messagesHistory.RemoveAt(messagesHistory.Count - 1);
			}

			//TODO: Can't get Usage from streamed response
			EnsureHistorySizeLimit(null);
			return assistantResponse;
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

		private async Task<string> ReadStreamedResponse(HttpResponseMessage response, Action<string>? onStreamedData = null)
		{
			var resultBuilder = new StringBuilder();
			onStreamedData ??= _ => {};

			using var stream = await response.Content.ReadAsStreamAsync();
			using var reader = new StreamReader(stream);

			while (!reader.EndOfStream)
			{
				var line = await reader.ReadLineAsync();
				if (string.IsNullOrWhiteSpace(line))
				{
					continue;
				}

				if (line.StartsWith("data:") && line.Length >= 5)
				{
					line = line[5..].Trim();
					if (line == "[DONE]")
					{
						break;
					}
				}

				try
				{
					var json = JsonDocument.Parse(line);
					var responseDelta = json.RootElement
						.GetProperty("choices")[0]
						.GetProperty("delta");

					if (responseDelta.TryGetProperty("content", out var content))
					{
						
						var contentText = content.GetString() ?? string.Empty;
						resultBuilder.Append(contentText);
						onStreamedData(contentText);
					}
				}
				catch (Exception ex)
				{
					throw new Exception($"Stream exeption: {ex.Message}");
				}
			}

			return resultBuilder.ToString();
		}
		private async Task<string> ReadDirectResponse(HttpResponseMessage response)
		{
			var responseData = await response.Content.ReadFromJsonAsync<ResponseData>();
			if (responseData?.Choices == null || responseData.Choices.Count == 0)
			{
				throw new Exception("No choices were returned by the API");
			}

			return responseData.Choices[0].Message.Content?.Trim() ?? string.Empty;
		}

		private void EnsureHistorySizeLimit(Usage? usage)
		{
			var totalTokens = usage?.TotalTokens ?? CurrentTokenHistorySize;
			sessionTokenUsage += totalTokens;
			return;
			if (totalTokens <= MaxTokenHistorySize)
			{
				return;
			}

			HistoryOverflowNotify?.Invoke(messagesHistory);
			ProcessApiUsageResponse(usage);
		}

		private void ProcessApiUsageResponse(Usage usage)
		{
			if (messagesHistory.Count > 1)
			{
				var firstMessageRole = messagesHistory.First().Role;
				bool hasSystemPrompt = firstMessageRole == DialogRole.System || firstMessageRole == DialogRole.Developer;
				int targetTokens = SessionTokenUsage / 2;
				int startIndex = hasSystemPrompt ? 1 : 0;

				while (CurrentTokenHistorySize > targetTokens && messagesHistory.Count > startIndex)
				{
					messagesHistory.RemoveAt(startIndex);
				}
			}
		}

		private int RecalculateHistoryTokens()
		{
			return messagesHistory.Sum(msg => tokenCalculator.ConvertCharactersToTokens(msg.Content));
		}
	}
}
