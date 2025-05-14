namespace GPTProject.Providers.Dialogs.Interfaces
{
	public interface ITokenCalculator
	{
		int ConvertCharactersToTokens(string text);
	}
}
