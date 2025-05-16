using GPTProject.Providers.Data.Vectorizers;

namespace GPTProject.Providers.Vectorizers.Interfaces
{
	public interface IVectorizer
	{
		Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request);
		void SetModel(string model);
	}
}
