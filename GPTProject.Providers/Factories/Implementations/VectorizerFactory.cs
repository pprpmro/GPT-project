using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Factories.Interfaces;
using GPTProject.Providers.Vectorizers.Implementation;
using GPTProject.Providers.Vectorizers.Interfaces;
using static GPTProject.Providers.Common.Configurations;

namespace GPTProject.Providers.Factories.Implementations
{
	public class VectorizerFactory : IVectorizerFactory
	{
		public IVectorizer Create(Embedder embedder)
		{
			IVectorizer vectorizer = embedder.Provider switch
			{
				ProviderType.ChatGPT => new ChatGPTVectorizer(),
				ProviderType.YandexGPT => new YandexGPTVectorizer(),
				ProviderType.GigaChat => new GigaChatVectorizer(),
				_ => throw new NotSupportedException()
			};

			vectorizer.SetModel(embedder.Model);
			return vectorizer;
		}
	}
}
