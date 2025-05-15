using System.Text.Json.Serialization;
using System.Text.Json;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Common;

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
		public ProviderType Provider { get; set; }
		public required string EmbedderModel { get; set; }

		public Character(string name, string description, string? example = null, string? plans = null, string? summary = null) 
		{
			Name = name;
			Description = description;
			Example = example;
			Plans = plans;
			Summary = summary;
		}
	}
}
