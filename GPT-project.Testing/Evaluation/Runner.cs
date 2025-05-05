using GPTProject.Testing.Metrics;

namespace GPTProject.Testing.Evaluation
{
	public class Runner
	{
		private readonly BERTScore bertScoreRunner;
		private readonly SemanticSimilarityScore cosineScorer;
		private readonly LLMAsJudgeScore llmJudgeScorer;

		private readonly object locker = new();
		private readonly SemaphoreSlim _throttle = new SemaphoreSlim(1);
		private const int RequestIntervalMs = 150;

		public Runner(BERTScore bertScoreRunner, SemanticSimilarityScore cosineScorer, LLMAsJudgeScore llmJudgeScorer)
		{
			this.bertScoreRunner = bertScoreRunner;
			this.cosineScorer = cosineScorer;
			this.llmJudgeScorer = llmJudgeScorer;
		}

		public async Task<Result> EvaluateAsync(List<TestItem> pairs)
		{
			var bertScores = new List<double>();
			var cosineScores = new List<double>();

			var llmTasks = new List<Task>();
			var judgeScores = new List<double>();

			foreach (var pair in pairs)
			{
				var bertScore = bertScoreRunner.CalculateScore(pair.Generated, pair.Reference);
				var cosineScore = await cosineScorer.CalculateScoreAsync(pair.Generated, pair.Reference);

				bertScores.Add(bertScore);
				cosineScores.Add(cosineScore);

				//var task = CalculateJudgeScore(pair);
				//llmTasks.Add(task);
			}
			//await Task.WhenAll(llmTasks);

			return new Result
			{
				PairCount = pairs.Count,
				BertScore = bertScores,
				CosineScore = cosineScores,
				JudgeScore = judgeScores
			};

			Task CalculateJudgeScore(TestItem pair)
			{
				return Task.Run(async () =>
				{
					await _throttle.WaitAsync();
					try
					{
						var score = await llmJudgeScorer.CalculateScoreAsync(pair.Question, pair.Reference, pair.Generated);
						lock (locker)
						{
							judgeScores.Add((double)score);
						}
					}
					catch
					{
						lock (locker)
						{
							judgeScores.Add(0.0);
						}
					}
					finally
					{
						await Task.Delay(RequestIntervalMs);
						_throttle.Release();
					}
				});
			}
		}

	}
}
