
using System.Net.Http.Json;
using System.Text.Json;
using GPTTest.Providers.GigaChat.Common;

namespace GPTTest.Providers.GigaChat
{
    public class GigaChatDialog
    {
        private List<Message> messagesHistory;
        private HttpClient httpClient;
        private AccessData? accessData;
        private const int minimalContentLength = 1;
        private Guid RqUID;


        public GigaChatDialog()
        {
            messagesHistory = new List<Message>();
            httpClient = new HttpClient();
            RqUID = Guid.NewGuid();
        }

        private async Task<AccessData> GetAccessData()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Settings.authorizeData}");
            httpClient.DefaultRequestHeaders.Add("RqUID", RqUID.ToString());

            var scopeList = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("scope", Settings.scope) };
            using var response = await httpClient.PostAsync(Settings.getAccessTokenEndpoint, new FormUrlEncodedContent(scopeList));

            var accessData = GetAccessDataFromResponseMessage(response);

            if (accessData == null)
            {
                throw new NullReferenceException(nameof(accessData));
            }

            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessData.AcessToken}");
            return accessData;
        }

        private AccessData? GetAccessDataFromResponseMessage(HttpResponseMessage? response)
        {
            if (response == null)
            {
                throw new NullReferenceException(nameof(response));
            }

            string result = response.Content.ReadAsStringAsync().Result;
            if (response.IsSuccessStatusCode)
            {
                return JsonSerializer.Deserialize<AccessData>(result);
            }
            else
            {
                throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
            }
        }

        public async Task GetCompletionsMessage(string role, string content)
        {
            if (content.Length < minimalContentLength)
            {
                throw new ArgumentException("Message length is less than minimum");
            }

            if (accessData == null || accessData.isExpired)
            {
                var newAccessData = await GetAccessData();
                accessData = newAccessData;
            }

            var message = new Message() { Role = role, Content = content };
            messagesHistory.Add(message);

            var Request = new Request()
            {
                ModelId = "GigaChat:latest",
                Messages = messagesHistory
            };

            using var response = await httpClient.PostAsJsonAsync(Settings.getCompletionsEndpoint, Request);

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

            var choice = choices[0];
            var responseMessage = choice.Message;
            messagesHistory.Add(responseMessage);
            var responseText = responseMessage.Content.Trim();
            Console.WriteLine($"GigaChat: {responseText}");
        }
    }
}
