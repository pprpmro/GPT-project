namespace GPTProject.Providers.Common
{
	public static class EnvironmentLoader
	{
		private static readonly string DefaultEnvContent =
		"# ChatGPT" + Environment.NewLine +
		"CHATGPT_API_KEY=PutYourKeyHere" + Environment.NewLine +
		"" + Environment.NewLine +
		"# DeepSeek" + Environment.NewLine +
		"DEEPSEEK_API_KEY=PutYourKeyHere" + Environment.NewLine +
		"" + Environment.NewLine +
		"# YandexGPT" + Environment.NewLine +
		"YANDEXGPT_API_KEY=PutYourKeyHere" + Environment.NewLine +
		"YANDEXGPT_CATALOG_ID=PutYourKeyHere" + Environment.NewLine +
		"YANDEXGPT_KEY_ID=PutYourKeyHere" + Environment.NewLine +
		"" + Environment.NewLine +
		"# GigaChat" + Environment.NewLine +
		"GIGACHAT_AUTHORIZE_DATA=PutYourKeyHere" + Environment.NewLine;

		private const string defaultEnvFilePath = ".env";


		public static string GetEnvironmentVariable(string variableKey, string filePath = defaultEnvFilePath)
		{
			string? value = Environment.GetEnvironmentVariable(variableKey);
			if (!string.IsNullOrEmpty(value))
			{
				return value;
			}

			value = LoadFromEnvFile(variableKey, filePath);
			if (!string.IsNullOrEmpty(value))
			{
				Environment.SetEnvironmentVariable(variableKey, value);
				return value;
			}
			throw new InvalidOperationException($"Переменная окружения '{variableKey}' не найдена ни в окружении, ни в файле {filePath}.");
		}


		public static string LoadFromEnvFile(string variableKey, string filePath)
		{
			if (!File.Exists(filePath))
			{
				Console.WriteLine("File .env does not exist, creating a new one...");
				File.WriteAllText(filePath, DefaultEnvContent);
				Console.WriteLine();
				throw new InvalidOperationException($".env created. Path: {Path.GetFullPath(filePath)}. Fill it");
			}

			var requiredVariables = new List<string>();

			foreach (var line in File.ReadAllLines(filePath))
			{
				if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
				{
					continue;
				}

				var separatorIndex = line.IndexOf('=');
				if (separatorIndex == -1)
				{
					continue;
				}

				var envKey = line.Substring(0, separatorIndex).Trim();
				var envValue = line.Substring(separatorIndex + 1).Trim();

				if (string.Equals(envKey, variableKey, StringComparison.OrdinalIgnoreCase))
				{
					return envValue;
				}
			}

			return string.Empty;
		}
	}

}
