using System.Text.Json.Serialization;
using GPTProject.Core.Interfaces;

namespace GPTProject.Core.Models.Common
{
	public class Message : IMessage
	{
		[JsonPropertyName("role")]
		public string Role { get; set; } = "";

		[JsonPropertyName("content")]
		public string Content { get; set; } = "";
	}
}
