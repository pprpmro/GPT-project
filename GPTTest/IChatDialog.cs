namespace GPTProject.Core
{
    public interface IChatDialog
    {
        ///<summary>
        ///Will return first result
        ///</summary>
        Task<string> SendMessage(string message);
        void ClearDialog(bool clearSystemPrompt = true);
        void SetSystemPrompt(string message, bool clearDialog = true);
    }
}
