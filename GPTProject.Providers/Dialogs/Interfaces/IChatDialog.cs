using GPTProject.Providers.Data.Dialogs;

namespace GPTProject.Providers.Dialogs.Interfaces
{
	public interface IChatDialog
	{
		Task<string> SendMessage(string? message, Action<string>? onStreamedData = null, bool stream = true, bool rememberMessage = true);
		void ClearDialog(bool clearSystemPrompt = true);
		void ClearDialog(bool clearSystemPrompt = false, int? lastNMessages = null);
		void UpdateSystemPrompt(string message, bool clearDialog = false);
		void SetOverflowHandler(Action<List<IMessage>> handler);
		void SetCustomDialog(List<IMessage> customMessagesHistory);
		List<IMessage> GetDialog();
		int MaxDialogHistorySize { get; set; }
		int TotalSendedCharacterCount { get; set; }

	}
}
