namespace GPTProject.Core.Interfaces.Vectorizers
{
	public interface IVectorizerResponse
	{
		float[][] Embedding { get; set; }
	}
}
