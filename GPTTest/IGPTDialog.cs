namespace GPTProject.Core
{
    public interface IGPTDialog
    {
        Task<string> SendUserMessageAndGetFirstResult(string message);
        void ClearDialog();
        void SetSystemPrompt(string message, bool clearDialog = true);
    }
}
