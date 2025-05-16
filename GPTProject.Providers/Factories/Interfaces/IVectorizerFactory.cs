using GPTProject.Providers.Vectorizers.Interfaces;
using static GPTProject.Providers.Common.Configurations;

namespace GPTProject.Providers.Factories.Interfaces
{
	public interface IVectorizerFactory
	{
		IVectorizer Create(Embedder embedder);
	}
}
