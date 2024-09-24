using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPTTest.Providers.YandexGPT
{
    public static class Settings
    {
        public const string OAuthToken = "y0_AgAAAAA21-EMAATuwQAAAAD5To6pAAAvRzdVgkVFdL1HQouTbuCPm_3f_w";

        public static readonly string getCompletionsEndpoint = "https://gigachat.devices.sberbank.ru/api/v1/chat/completions";
        public static readonly string getAccessTokenEndpoint = "https://iam.api.cloud.yandex.net/iam/v1/tokens";

    }
}
