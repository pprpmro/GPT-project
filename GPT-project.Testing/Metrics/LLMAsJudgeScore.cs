using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Testing.Metrics
{
	public class LLMAsJudgeScore
	{
		private readonly IChatDialog dialog;

		public LLMAsJudgeScore(IChatDialog dialog)
		{
			this.dialog = dialog;
			this.dialog.UpdateSystemPrompt(GetSystemPrompt());
		}

		public async Task<int?> CalculateScoreAsync(string question, string reference, string generated)
		{
			string prompt =
	$@"Вопрос: {question} {Environment.NewLine}
Эталонный ответ: {reference} {Environment.NewLine}
Сгенерированный ответ: {generated} {Environment.NewLine}
Ответь только цифрой.";

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

		private static string GetSystemPrompt()
		{
			return $"Вы выступаете в роли независимого эксперта по оценке качества текстов, сгенерированных языковыми моделями." +
				$"Ваша задача — сравнить сгенерированный ответ с эталонным, основываясь на его структурной чёткости, лаконичности, ясности изложения и отсутствии избыточной или дублирующей информации." +
				$"Оцените сгенерированный ответ по 5-балльной шкале, где: {Environment.NewLine}" +
				$"5 — идеально соответствует эталону по структуре и подаче, максимально чётко и лаконично, без лишних фрагментов; {Environment.NewLine}" +
				$"4 — в целом чётко, но есть незначительная избыточность или перегруженность формулировок; {Environment.NewLine}" +
				$"3 — заметно перегружено или слабо структурировано, хотя суть соблюдена; {Environment.NewLine}" +
				$"2 — есть существенная избыточность, многословие или путаность; {Environment.NewLine}" +
				$"1 — ответ чрезмерно размыт, плохо читается или почти полностью состоит из лишней информации. {Environment.NewLine}" +
				$"Важно: не проверяйте фактическую достоверность ответа, оценивайте только форму подачи, стиль и уместность содержания.";
		}
	}
}
