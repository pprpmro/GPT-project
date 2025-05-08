using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;

namespace GPTProject.Testing.Metrics
{
	public class SemanticSimilarityScore
	{
		private readonly IVectorizer _vectorizer;

		public SemanticSimilarityScore(IVectorizer vectorizer)
		{
			_vectorizer = vectorizer;
		}

		public async Task<double> CalculateScoreAsync(string generatedText, string referenceText)
		{
			var generatedTextRequest = new VectorizerRequest()
			{
				Encoding_format = "float",
				Model = "text-embedding-3-small",
			};
			generatedTextRequest.Input = new string[] { generatedText };

			var referenceTextRequest = new VectorizerRequest()
			{
				Encoding_format = "float",
				Model = "text-embedding-3-small",
			};
			referenceTextRequest.Input = new string[] { referenceText };

			var generatedEmbedding = await _vectorizer.GetEmbeddingAsync(generatedTextRequest);
			var referenceEmbedding = await _vectorizer.GetEmbeddingAsync(referenceTextRequest);

			return ComputeCosineSimilarity(generatedEmbedding.Embedding[0], referenceEmbedding.Embedding[0]);
		}

		public double ComputeCosineSimilarity(float[] vectorA, float[] vectorB)
		{
			if (vectorA.Length != vectorB.Length)
			{
				throw new ArgumentException("Vectors must be of the same dimension");
			}

			double dotProduct = 0.0;
			double magnitudeA = 0.0;
			double magnitudeB = 0.0;

			for (int i = 0; i < vectorA.Length; i++)
			{
				dotProduct += vectorA[i] * vectorB[i];
				magnitudeA += Math.Pow(vectorA[i], 2);
				magnitudeB += Math.Pow(vectorB[i], 2);
			}
			double denominator = Math.Sqrt(magnitudeA) * Math.Sqrt(magnitudeB);

			return denominator == 0 ? 0 : Math.Round(dotProduct / denominator, 4);
		}
	}
}
