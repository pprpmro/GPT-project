namespace GPTProject.Core
{
    public interface IGPTDialog
    {
        Task<string> SendUserMessageAndGetFirstResult(string content);

        void ClearDialog();

        void SetSystemPrompt(string message, bool clear = true);
    }
}
