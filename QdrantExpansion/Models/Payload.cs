namespace QdrantExpansion.Models
{
	public class Payload
	{
		public string Text { get; set; }
		public DateTime Date { get; set; }
		public float Importance { get; set; }

		public Dictionary<string, object?> GenerateDictionary()
		{
			return new Dictionary<string, object?> { ["text"] = Text, ["date"] = DateTime.Now.Date, ["importance"] = Importance };
		}
	}
}
