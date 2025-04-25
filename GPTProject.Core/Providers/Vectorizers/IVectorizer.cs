using GPTProject.Core.Models.Common;

namespace GPTProject.Core.Providers.Vectorizers
{
	public interface IVectorizer
	{
		Task<VectorizerResponse> GetEmbeddingAsync(VectorizerRequest request);
	}
}
