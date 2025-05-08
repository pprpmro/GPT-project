using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;
		private readonly string _collectionName;
		private readonly MemoryAgent _memoryAgent;

		private string _memories;

		public DialogueAgent(IChatDialog provider, string collectionName, VectorizerRequest request, string SystemPrompt = "") {
			_provider = provider;
			_collectionName = collectionName;
			_memoryAgent = new MemoryAgent(new ChatGPTDialog(), _collectionName, request);

			_provider.SetOverflowHandler(OverflowHandlerAsync);
			if (SystemPrompt != "") _provider.UpdateSystemPrompt(SystemPrompt);
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction, Action<string?> ReturnFunction)
		{
			while (true)
			{
				var userMessage = await GetUserMessageFunction();
				if(await CheckCommand(userMessage)) { continue; }

				_memories = await _memoryAgent.Restore(userMessage);

				ReturnFunction(await _provider.SendMessage(PromptManager.GetThisIsWhatYouRememberPrompt(_memories, userMessage)));
			}
		}

		private void OverflowHandlerAsync(List<IMessage> messagesHistory)
		{
			_memoryAgent.Save(_provider.GetDialog());
		}

		private async Task<bool> CheckCommand(string message)
		{
			switch (message)
			{
				case "/save":
					await _memoryAgent.Save(_provider.GetDialog());
					return true;

				default:
					return false;
			}
		}
	}
}
