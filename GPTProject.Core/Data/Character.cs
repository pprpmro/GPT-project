using GPTProject.Providers.Dialogs.Enumerations;

namespace GPTProject.Core.Data
{
	public class Character
	{
		// Основные поля персонажа
		public required string Name { get; set; }
		public required string Description { get; set; }
		public string? Example { get; set; }
		public string? Plans { get; set; }
		public string? Summary { get; set; }

		// Поля для эмбеддинга
		public required ProviderType Provider { get; set; }
		public required string EmbedderModel { get; set; }

		public Character(string name, string description, ProviderType providerType, string embedderModel, string? example = null, string? plans = null, string? summary = null) 
		{
			Name = name;
			Description = description;
			Example = example;
			Plans = plans;
			Summary = summary;

			Provider = providerType;
			EmbedderModel = embedderModel;
		}
	}
}
