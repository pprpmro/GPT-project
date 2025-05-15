using GPTProject.Common;
using GPTProject.Core.Data;
using GPTProject.Providers.Dialogs.Enumerations;

namespace GPTProject.Core.Services.Interfaces
{
	public interface ICharacterService
	{
		void SaveToJson(Character character);
		Character LoadFromJson(string characterName);
		List<string> GetCharNames(Dictionary<DialogType, ProviderType> providerTypes, DialogType dialogType);
		string? GetCharacterFilePath(string characterName);
	}
}
