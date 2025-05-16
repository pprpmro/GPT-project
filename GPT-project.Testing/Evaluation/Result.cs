namespace GPTProject.Testing.Evaluation
{
	public class Result 
	{
		public int PairCount { get; set; }

		public List<double> BertScore { get; set; } = new List<double>();
		public double BertScoreAvg { get { return Math.Round(BertScore.Average(), 4); } }
		public double BertScoreMin { get { return Math.Round(BertScore.Min(), 4); } }
		public double BertScoreMax { get { return Math.Round(BertScore.Max(), 4); } }

		public List<double> CosineScore { get; set; } = new List<double>();
		public double CosineAvg { get { return Math.Round(CosineScore.Average(), 4); } }
		public double CosineMin { get { return Math.Round(CosineScore.Min(), 4); } }
		public double CosineMax { get { return Math.Round(CosineScore.Max(), 4); } }

		public List<double> JudgeScore { get; set; } = new List<double>();
		public double JudgeAvg { get { return JudgeScore.Count > 0 ? Math.Round(JudgeScore.Average(), 4) : 0.0; } }
		public double JudgeMin { get { return JudgeScore.Count > 0 ? Math.Round(JudgeScore.Min(), 4) : 0.0; } }
		public double JudgeMax { get { return JudgeScore.Count > 0 ? Math.Round(JudgeScore.Max(), 4) : 0.0; } }

		public override string ToString()
		{
			var result = $"Pair count {PairCount} {Environment.NewLine}";

			result += $"BERTScore{Environment.NewLine}";
			for (int i = 0; i < BertScore.Count; i++)
			{
				result += $"{i+ 1}) Score: {BertScore[i]} {Environment.NewLine}";
			}
			result += $"avg={BertScoreAvg}, min={BertScoreMin}, max={BertScoreMax} {Environment.NewLine}";
			result += $"{Environment.NewLine}";

			result += $"Semantic Similarity:{Environment.NewLine}";
			for (int i = 0; i < CosineScore.Count; i++)
			{
				result += $"{i+1}) Score: {CosineScore[i]}{Environment.NewLine}";
			}
			result += $"avg={CosineAvg}, min={CosineMin}, max={CosineMax} {Environment.NewLine}";

			//result += $"{Environment.NewLine}";
			//result += $"LLM-as_Judge:{Environment.NewLine}";
			//for (int i = 0; i < JudgeScore.Count; i++)
			//{
			//	result += $"{i + 1}) Score: {JudgeScore[i]}{Environment.NewLine}";
			//}
			//result += $"avg={JudgeAvg}, min={JudgeMin}, max={JudgeMax} {Environment.NewLine}";

			return result ;
		}
	}
}
