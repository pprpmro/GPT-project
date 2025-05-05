using System.Diagnostics;
using System.Globalization;

namespace GPTProject.Testing.Metrics
{
	public class BERTScore : IDisposable
	{

		private readonly string scriptPath;

		private const string scriptFileName = "bert_score_temp.py";

		private const string pythonScript = @"
import sys
from bert_score import score

gen_text = sys.argv[1]
ref_text = sys.argv[2]

P, R, F1 = score([gen_text], [ref_text], lang='ru', verbose=False)
print(round(F1[0].item(), 4))
";

		public BERTScore()
		{
			scriptPath = Path.Combine(AppContext.BaseDirectory, scriptFileName);
			File.WriteAllText(scriptPath, pythonScript);
		}

		public double CalculateScore(string generated, string reference)
		{
			var psi = new ProcessStartInfo
			{
				FileName = "python",
				Arguments = $"\"{scriptPath}\" \"{Escape(generated)}\" \"{Escape(reference)}\"",
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

			if (process.ExitCode != 0 || !string.IsNullOrEmpty(error))
			{
				throw new Exception("Ошибка выполнения Python-скрипта: " + error);
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

		~BERTScore() => Dispose();

		public void Dispose()
		{
			if (File.Exists(scriptPath))
			{
				try
				{
					File.Delete(scriptPath);
				}
				catch
				{

				}
				GC.SuppressFinalize(this);
			}
		}
	}
}
