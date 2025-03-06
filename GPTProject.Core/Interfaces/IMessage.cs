namespace GPTProject.Core.Interfaces
{
	public interface IMessage
	{
		string Role { get; set; }
		string Content { get; set; }
	}
}
