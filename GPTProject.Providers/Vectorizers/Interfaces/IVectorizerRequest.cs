namespace GPTProject.Providers.Vectorizers.Interfaces
{
	public interface IVectorizerRequest
	{
		string Key { get; set; }
		string Url { get; set; }
		string[] Input { get; set; }
		string Model { get; set; }
		string Encoding_format { get; set; }
	}
}
