using GPTProject.Core.Interfaces;

namespace GPTProject.Testing.Metrics
{
	public class LLMAsJudgeScore
	{
		private readonly IChatDialog dialog;
		private readonly string systemPrompt = "Ты эксперт, оценивающий ответы. Тебе дан пользовательский вопрос, эталонный ответ и сгенерированный ответ.";

		public LLMAsJudgeScore(IChatDialog dialog)
		{
			this.dialog = dialog;
			this.dialog.UpdateSystemPrompt(this.systemPrompt);
		}

		public async Task<int?> CalculateScoreAsync(string question, string reference, string generated)
		{
			string prompt =
	$@"Вопрос: {question} {Environment.NewLine}
Эталонный ответ: {reference} {Environment.NewLine}
Сгенерированный ответ: {generated} {Environment.NewLine}

Оцени сгенерированный ответ по 5-балльной шкале, где 1 — полностью неверно, а 5 — абсолютно точно соответствует эталону. Ответь только цифрой.";

			try
			{
				string response = await dialog.SendMessage(prompt, rememberMessage: false);
				if (int.TryParse(response.Trim(), out int score) && score >= 1 && score <= 5)
				{
					return score;
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Ошибка при оценке LLM-as-a-Judge: {ex.Message}");
			}

			return null;
		}
	}
}
