using System.Diagnostics;
using System.Globalization;

namespace GPTProject.Testing.Metrics
{
	public class BERTScore
	{
		private const string ScriptFileName = "bert_score_temp.py";

		private const string PythonScript = @"
import sys
from bert_score import score

gen_text = sys.argv[1]
ref_text = sys.argv[2]

P, R, F1 = score([gen_text], [ref_text], lang='ru', verbose=False)
print(round(F1[0].item(), 4))
";
		public double CalculateScore(string generated, string reference)
		{
			File.WriteAllText(ScriptFileName, PythonScript);

			var psi = new ProcessStartInfo
			{
				FileName = "python",
				Arguments = $"{ScriptFileName} \"{Escape(generated)}\" \"{Escape(reference)}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
				CreateNoWindow = true
			};

			using var process = Process.Start(psi);
			if (process is null)
			{
				throw new Exception("Process is null");
			}

			string output = process.StandardOutput.ReadToEnd();
			string error = process.StandardError.ReadToEnd();
			process.WaitForExit();

			if (!string.IsNullOrWhiteSpace(error))
			{
				throw new Exception("Ошибка Python: " + error);
			}

			return TryParseScore(output.Trim());
		}

		private static string Escape(string input)
		{
			return input.Replace("\"", "\\\"");
		}

		public static double TryParseScore(string output)
		{
			if (double.TryParse(output.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double result))
			{
				return Math.Round(result, 4);
			}
			return 0.0;
		}
	}
}
