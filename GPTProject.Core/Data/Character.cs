using GPTProject.Providers.Dialogs.Enumerations;
using System.Text.Json.Serialization;

namespace GPTProject.Core.Data
{
	public class Character
	{
		// Основные поля персонажа
		public string Name { get; set; }
		public string Description { get; set; }
		public string? Example { get; set; }
		public string? Plans { get; set; }
		public string? Summary { get; set; }

		// Поля для эмбеддинга
		public string Provider { get; set; }
		public string EmbedderModel { get; set; }

		public bool EnablePlans { get; set; }
		public bool EnableSummary { get; set; }

		/*[JsonConstructor]
		public Character(string name, string description, string? example, string? plans, string? summary, string provider, string embedderModel, bool enablePlans, bool enableSummary) 
		{
			Name = name;
			Description = description;
			Example = example;
			Plans = plans;
			Summary = summary;

			Provider = provider;
			EmbedderModel = embedderModel;

			EnablePlans = enablePlans;
			EnableSummary = enableSummary;
		}*/

		public Character(string name, string description, string provider, string embedderModel, string? example = null, string? plans = null, string? summary = null, bool enablePlans = false, bool enableSummary = false)
		{
			Name = name;
			Description = description;
			Example = example;
			Plans = plans;
			Summary = summary;

			Provider = provider;
			EmbedderModel = embedderModel;

			EnablePlans = enablePlans;
			EnableSummary = enableSummary;
		}
	}
}
