namespace GPTProject.Providers.Dialogs.Interfaces
{
	public interface IRequest
	{
		int AnswerCount { get; set; }
		List<IMessage> Messages { get; set; }
		string Model { get; set; }
		double Temperature { get; set; }
		bool Stream {  get; set; }
	}
}