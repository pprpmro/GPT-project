namespace GPTProject.Common.Utils
{
	public static class Helper
	{
		private static readonly string[] unclearResponses =
		{
			"Я не могу обработать ваш запрос.",
			"Ваш вопрос некорректен или не относится к теме.",
			"Этот запрос выходит за рамки моей компетенции.",
			"Я не могу ответить на этот вопрос в текущем контексте.",
			"Уточните запрос, чтобы я мог вам помочь.",
			"Этот вопрос не соответствует тематике диалога.",
			"Попробуйте задать вопрос иначе.",
			"Ваш запрос не может быть обработан в текущей форме."
		};

		public static string GetRandomUnclearResponse()
		{
			var random = new Random();
			return unclearResponses[random.Next(unclearResponses.Length)];
		}
	}
}
