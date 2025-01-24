namespace GPTProject.Core
{
	public interface IChatDialog
	{
		///<summary>
		///Will return first result
		///</summary>
		Task<string> SendMessage(string message);
		void ClearDialog(bool clearSystemPrompt = true);

		///<summary>
		///Will clear all dialog
		///</summary>
		void SetSystemPrompt(string message);
		void ReplaceSystemPrompt(string message, bool clearDialog = true);
	}
}
