using GPTProject.Core.Interfaces;

namespace GPTProject.Core.Providers
{
	public abstract class BaseChatDialog<TMessage> : IChatDialog where TMessage : IMessage, new()
	{
		protected List<TMessage> messagesHistory = new();
		protected HttpClient httpClient = new();
		protected const int MinimalContentLength = 1;

		public int MaxDialogHistorySize { get; set; }
		public int TotalSendedCharacterCount { get; set; }

		public int CurrentHistorySymbolsCount { get { return messagesHistory.Where(x => x.Content != null).Select(x => x.Content.Length).Sum(); } }

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

		public abstract Task<string> SendMessage(string message, bool rememberMessage = true);

		public int GetHistoryCharacterCount()
		{
			return messagesHistory.Sum(m => m.Content.Length);
		}
	}
}
