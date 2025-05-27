using System.Text.Json;
using System.Text.RegularExpressions;

namespace GPTProject.Common.Utils
{
	public static class JsonUtil
	{
		private static readonly Regex JsonRegex = new Regex(
			@"(?<json>\{(?:[^{}]|(?<open>\{)|(?<-open>\}))*\}(?(open)(?!))|(?<json>\[(?:[^\[\]]|(?<open>\[)|(?<-open>\]))*\](?(open)(?!))))",
			RegexOptions.Compiled | RegexOptions.Singleline
		);

		public static string? ExtractJsonFromString(string dirtyString)
		{
			if (string.IsNullOrWhiteSpace(dirtyString))
				return null;

			var matches = JsonRegex.Matches(dirtyString);

			foreach (Match match in matches)
			{
				string potentialJson = match.Groups["json"].Value;

				if (IsValidJson(potentialJson))
				{
					return potentialJson;
				}
			}

			return null;
		}

		private static bool IsValidJson(string jsonCandidate)
		{
			try
			{
				using (JsonDocument.Parse(jsonCandidate))
				{
					return true;
				}
			}
			catch (JsonException)
			{
				return false;
			}
		}
	}
}
