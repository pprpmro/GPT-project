using System.Net.Http.Json;
using GPTProject.Common;

namespace GPTProject.Core.Providers.ChatGPT
{
    public class ChatGPTDialog : IChatDialog
    {

        private List<Message> messagesHistory;
        private HttpClient httpClient;
        private const int minimalContentLength = 1;

        public int MessageCount { get { return messagesHistory.Count; } }

        public ChatGPTDialog()
        {
            messagesHistory = new List<Message>();
            httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {Settings.apiKey}");
        }

        public void ClearDialog(bool clearSystemPrompt = true)
        {
            if (clearSystemPrompt)
            {
                messagesHistory.Clear();
            }
            else
            {
                messagesHistory = messagesHistory.Where(x => x.Role == Role.System).ToList();
            }
        }

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

        public async Task<string> SendMessage(string message)
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

            using var response = await httpClient.PostAsJsonAsync(Settings.completionsEndpoint, requestData);

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
