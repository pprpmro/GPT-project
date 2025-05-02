using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Vectorizers.Interfaces;

namespace GPTProject.Testing.Metrics
{
	public class CosineSimilarityScore
	{
		private readonly IVectorizer _vectorizer;

		public CosineSimilarityScore(IVectorizer vectorizer)
		{
			_vectorizer = vectorizer;
		}

		public async Task<double> CalculateScoreAsync(string generatedText, string referenceText)
		{
			var generatedTextRequest = new VectorizerRequest()
			{
				Key = "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",
				Url = "https://api.openai.com/v1/embeddings",
				Encoding_format = "float",
				Model = "text-embedding-3-small",
			};
			generatedTextRequest.Input = new string[] { generatedText };

			var referenceTextRequest = new VectorizerRequest()
			{
				Key = "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",
				Url = "https://api.openai.com/v1/embeddings",
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
