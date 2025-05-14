using System.Collections.Concurrent;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Providers.Dialogs.Implementations
{
	public class TokenCalculator : ITokenCalculator
	{
		private static readonly Dictionary<LanguageType, double> tokenRatios = new()
		{
			{ LanguageType.English, 4.0 },
			{ LanguageType.Russian, 3.0 },
			//{ LanguageType.Chinese, 1.5 },
			//{ LanguageType.Japanese, 1.2 },
			//{ LanguageType.Unknown, 4.0 }
		};

		private static readonly double WorstPossibleRatio = tokenRatios.Values.Min();
		private readonly ConcurrentDictionary<string, int> tokenCache = new();

		public int ConvertCharactersToTokens(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return 0;
			}

			if (tokenCache.TryGetValue(text, out int cachedTokens))
			{
				return cachedTokens;
			}

			var worstRatio = DetectWorstLanguageRatioParallel(text);
			var tokenCount = (int)Math.Ceiling(text.Length / worstRatio);

			tokenCache[text] = tokenCount;
			return tokenCount;
		}

		private double DetectWorstLanguageRatioParallel(string text)
		{
			double currentWorstRatio = double.MaxValue;
			Parallel.For(0, text.Length, (i, state) =>
			{
				var lang = DetectLanguage(text[i]);
				if (tokenRatios.TryGetValue(lang, out double ratio))
				{
					lock (tokenRatios)
					{
						if (ratio < currentWorstRatio)
						{
							currentWorstRatio = ratio;
						}

						if (currentWorstRatio == WorstPossibleRatio)
						{
							state.Stop();
						}
					}
				}
			});
			return currentWorstRatio == double.MaxValue ? tokenRatios[LanguageType.Unknown] : currentWorstRatio;
		}

		private LanguageType DetectLanguage(char ch) => ch switch
		{
			>= 'A' and <= 'Z' or >= 'a' and <= 'z' => LanguageType.English,
			>= 'А' and <= 'я' or >= 'Ё' and <= 'ё' => LanguageType.Russian,
			>= '\u4E00' and <= '\u9FFF' => LanguageType.Chinese,
			>= '\u3040' and <= '\u30FF' => LanguageType.Japanese,
			_ => LanguageType.Unknown
		};
	}
}
