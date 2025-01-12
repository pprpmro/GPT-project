using System;


namespace GPTTest.Providers.YandexGPT
{
    public class YandexGPTDialog
    {
        //private List<Message> messagesHistory;
        private HttpClient httpClient;
        //private AccessData? accessData;
        private const int minimalContentLength = 1;
        private Guid RqUID;

        private async Task GetAccessData()
        {
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {Settings.OAuthToken}");
            httpClient.DefaultRequestHeaders.Add("RqUID", RqUID.ToString());

            using var response = await httpClient.PostAsync(Settings.accessTokenEndpoint, new StringContent(Settings.OAuthToken));

            //var accessData = GetAccessDataFromResponseMessage(response);

            //if (accessData == null)
            //{
            //    throw new NullReferenceException(nameof(accessData));
            //}

            //httpClient.DefaultRequestHeaders.Clear();
            //httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {accessData.AcessToken}");
        }

        //private AccessData? GetAccessDataFromResponseMessage(HttpResponseMessage? response)
        //{
        //    if (response == null)
        //    {
        //        throw new NullReferenceException(nameof(response));
        //    }

        //    string result = response.Content.ReadAsStringAsync().Result;
        //    if (response.IsSuccessStatusCode)
        //    {
        //        return JsonSerializer.Deserialize<AccessData>(result);
        //    }
        //    else
        //    {
        //        throw new Exception($"{(int)response.StatusCode} {response.StatusCode}");
        //    }
        //}
    }
}
