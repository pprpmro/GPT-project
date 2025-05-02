using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;
		public string _systemPrompt;

		public DialogueAgent(IChatDialog provider, string SystemPrompt = "") {
			_provider = provider;
			_systemPrompt = SystemPrompt;
			_provider.SetOverflowHandler(OverflowHandler);
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction)
		{
			while (true)
			{
				var userMessage = await GetUserMessageFunction();
				Console.WriteLine(_provider.SendMessage(userMessage));
			}
		}

		void OverflowHandler(List<IMessage> messagesHistory)
		{
			Console.WriteLine("Overflow");
		}
	}
}
