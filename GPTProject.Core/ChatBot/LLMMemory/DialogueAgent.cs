using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;

		public DialogueAgent(IChatDialog provider, string SystemPrompt = "") {
			_provider = provider;

			_provider.SetOverflowHandler(OverflowHandler);
			if (SystemPrompt != "") _provider.UpdateSystemPrompt(SystemPrompt);
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction, Action<string?> ReturnFunction)
		{
			while (true)
			{
				var userMessage = await GetUserMessageFunction();
				ReturnFunction(await _provider.SendMessage(userMessage));
			}
		}

		void OverflowHandler(List<IMessage> messagesHistory)
		{
			Console.WriteLine("Overflow"); //поменяется
		}
	}
}
