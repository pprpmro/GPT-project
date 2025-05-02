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
		public double JudgeAvg { get { return Math.Round(JudgeScore.Average(), 4); } }
		public double JudgeMin { get { return Math.Round(JudgeScore.Min(), 4); } }
		public double JudgeMax { get { return Math.Round(JudgeScore.Max(), 4); } }

		public override string ToString()
		{
			return
				$"Pair count {PairCount} {Environment.NewLine}" +
				$"BERTScore: avg={BertScoreAvg}, min={BertScoreMin}, max={BertScoreMax} {Environment.NewLine}" +
				$"Cosine: avg={CosineAvg}, min={CosineMin}, max={CosineMax} {Environment.NewLine}" +
				$"Judge: avg={JudgeAvg}, min={JudgeMin}, max={JudgeMax}";
		}
	}
}
