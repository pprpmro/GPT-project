namespace GPTProject.Core
{
	public interface IChatDialog
	{
		Task<string> SendMessage(string message, bool rememberMessage = true);
		void ClearDialog(bool clearSystemPrompt = true);
		void ClearDialog(bool clearSystemPrompt = false, int? lastNMessages = null);
		void UpdateSystemPrompt(string message, bool clearDialog = false);
		int MaxDialogHistorySize { get; set; }
	}
}
