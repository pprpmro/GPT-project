﻿using System.Text.Json.Serialization;

namespace GPTProject.Providers.Data.Dialogs
{
	public class Usage
	{
		[JsonPropertyName("prompt_tokens")]
		public int PromptTokens { get; set; }

		[JsonPropertyName("completion_tokens")]
		public int CompletionTokens { get; set; }

		[JsonPropertyName("total_tokens")]
		public int TotalTokens { get; set; }
	}
}
