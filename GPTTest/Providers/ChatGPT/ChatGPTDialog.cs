using System.Net.Http.Json;
using GPTProject.Core;
using GPTTest.Common;

namespace GPTTest.Providers.ChatGPT
{
    public class ChatGPTDialog : IGPTDialog
    {
        private const string apiKey = "sk-proj-E4Jev0ifLv4GVhPmZqKCT3BlbkFJE4v11lxBvMfwYHl18vpX";
        private const string endpoint = "https://api.openai.com/v1/chat/completions";

        private List<Message> messagesHistory = new List<Message>();
        private HttpClient httpClient;


        private const int minimalContentLength = 1;

        public int MessageCount { get { return messagesHistory.Count; } }

        public ChatGPTDialog()
        {
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
        }

        public void ClearDialog() => messagesHistory.Clear();

        public void SetSystemPrompt(string message, bool clear = true)
        {
            if (clear)
            {
                ClearDialog();
            }

            if (string.IsNullOrEmpty(message))
            {
                throw new ArgumentException("message is null");
            }

            messagesHistory.Add(new Message() { Role = Role.System, Content = message});
        }

        public async Task<string> SendUserMessageAndGetFirstResult(string message)
        {
            if (message.Length < minimalContentLength)
            {
                throw new ArgumentException("Message length is less than minimum");
            }

            messagesHistory.Add(new Message()
            {
                Role = Role.User,
                Content = message
            });

            var requestData = new Request()
            {
                ModelId = "gpt-3.5-turbo",
                Messages = messagesHistory
            };

            using var response = await httpClient.PostAsJsonAsync(endpoint, requestData);

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

            return choices[0].Message.Content.Trim();
        }
    }
}
