using System.Text.Json.Serialization;

namespace GPTProject.Core
{
    public class HelperResponse
	{
		[JsonPropertyName("clarificationQuestion")]
		public string? ClarificationQuestion { get; set; }
		[JsonPropertyName("response")]
		public string? Response { get; set; }
		[JsonPropertyName("needСlarification")]
		public bool NeedСlarification { get; set; }
	}
}