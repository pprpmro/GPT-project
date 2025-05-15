using GPTProject.Common;
using GPTProject.Core.Data;
using GPTProject.Core.Services.Interfaces;
using GPTProject.Providers.Dialogs.Enumerations;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GPTProject.Core.Services.Implementations
{
	public class CharacterService : ICharacterService
	{
		private JsonSerializerOptions options;
		private static readonly string folderName = "characters";

		public CharacterService()
		{
			options = new JsonSerializerOptions
			{
				WriteIndented = true,
				Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
				Converters = { new JsonStringEnumConverter() }
			};
		}

		// Метод для сохранения персонажа в JSON
		public void SaveToJson(Character character)
		{
			string charactersFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "characters");
			Directory.CreateDirectory(charactersFolder); // Создаем папку, если её нет

			string fileName = $"char-{character.Name}.json";
			string fullPath = Path.Combine(charactersFolder, fileName);

			string json = JsonSerializer.Serialize(this, options);
			File.WriteAllText(fullPath, json);
		}

		// Статический метод для загрузки персонажа из JSON
		public Character LoadFromJson(string characterName)
		{
			string charactersFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "characters");
			string fileName = $"char-{characterName}.json";
			string fullPath = Path.Combine(charactersFolder, fileName);

			if (!File.Exists(fullPath))
			{
				throw new FileNotFoundException($"Файл персонажа {characterName} не найден");
			}

			string json = File.ReadAllText(fullPath);

			return JsonSerializer.Deserialize<Character>(json, options);
		}

		public List<string> GetCharNames(Dictionary<DialogType, ProviderType> providerTypes, DialogType dialogType)
		{
			string appDirectory = AppDomain.CurrentDomain.BaseDirectory;
			string charactersFolder = Path.Combine(appDirectory, folderName);

			List<string> extractedNames = new List<string>();

			try
			{
				if (!Directory.Exists(charactersFolder))
				{
					throw new Exception("\"charachers\" folder not found");
				}

				string[] allJsonFiles = Directory.GetFiles(charactersFolder, "*.json");

				extractedNames = allJsonFiles
					.Select(Path.GetFileName)
					.Where(fileName => fileName.StartsWith("char-") && fileName.EndsWith(".json"))
					.Select(fileName => fileName.Substring(5, fileName.Length - 10))
					.ToList();

				return extractedNames;
			}
			catch (Exception ex)
			{
				throw new Exception(ex.Message);
			}
		}

		public string? GetCharacterFilePath(string characterName)
		{
			string charactersFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, folderName);
			string fileName = $"char-{characterName}.json";
			string fullPath = Path.Combine(charactersFolder, fileName);

			return File.Exists(fullPath) ? fullPath : null;
		}
	}
}
