namespace GPTProject.Core.Utils
{
	public static class EnvLoader
	{
		private static readonly string DefaultEnvContent =
		"# ChatGPT" + Environment.NewLine +
		"CHATGPT_API_KEY=" + Environment.NewLine +
		"CHATGPT_COMPLETIONS_ENDPOINT=https://api.openai.com/v1/chat/completions" + Environment.NewLine +
		"CHATGPT_MODEL=gpt-3.5-turbo-0125" + Environment.NewLine +
		"" + Environment.NewLine +
		"# YandexGPT" + Environment.NewLine +
		"YANDEXGPT_API_KEY=" + Environment.NewLine +
		"YANDEXGPT_COMPLETIONS_ENDPOINT=https://llm.api.cloud.yandex.net/foundationModels/v1/completion" + Environment.NewLine +
		"YANDEXGPT_CATALOG_ID=" + Environment.NewLine +
		"YANDEXGPT_KEY_ID=" + Environment.NewLine +
		"YANDEXGPT_MODEL=yandexgpt-lite" + Environment.NewLine +
		"" + Environment.NewLine +
		"# GigaChat" + Environment.NewLine +
		"GIGACHAT_AUTHORIZE_DATA=" + Environment.NewLine +
		"GIGACHAT_SCOPE=GIGACHAT_API_PERS" + Environment.NewLine +
		"GIGACHAT_ACCESS_TOKEN_ENDPOINT=https://ngw.devices.sberbank.ru:9443/api/v2/oauth" + Environment.NewLine +
		"GIGACHAT_COMPLETIONS_ENDPOINT=https://gigachat.devices.sberbank.ru/api/v1/chat/completions" + Environment.NewLine +
		"GIGACHAT_MODEL=GigaChat:latest" + Environment.NewLine;

		public static void Load(string filePath = ".env")
		{
			if (!File.Exists(filePath))
			{
				Console.WriteLine("File .env does not exist, creating a new one...");
				File.WriteAllText(filePath, DefaultEnvContent);
				Console.WriteLine($".env created. Path: {Path.GetFullPath(filePath)}");
			}

			var requiredVariables = new List<string>();

			foreach (var line in File.ReadAllLines(filePath))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
					continue;

				var separatorIndex = line.IndexOf('=');
				if (separatorIndex == -1) continue;

				var key = line.Substring(0, separatorIndex).Trim();
				var value = line.Substring(separatorIndex + 1).Trim();

				if (string.IsNullOrWhiteSpace(value))
				{
					requiredVariables.Add(key);
					continue;
				}

				Environment.SetEnvironmentVariable(key, value);
			}

			if (requiredVariables.Count > 0)
			{
				throw new Exception($"Error: empty variables in .env file: {string.Join(", ", requiredVariables)}");
			}

			Console.WriteLine("Variables is loaded from .env");
		}
	}

}
