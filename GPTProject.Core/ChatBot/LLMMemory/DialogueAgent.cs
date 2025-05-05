using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;
		private readonly string _collectionName;
		private readonly RememberingAgent _remAgent;

		public DialogueAgent(IChatDialog provider, string collectionName, VectorizerRequest request, string SystemPrompt = "") {
			_provider = provider;
			_collectionName = collectionName;
			_remAgent = new RememberingAgent(_provider, _collectionName, request);

			_provider.SetOverflowHandler(OverflowHandlerAsync);
			if (SystemPrompt != "") _provider.UpdateSystemPrompt(SystemPrompt);
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction, Action<string?> ReturnFunction)
		{
			while (true)
			{
				var userMessage = await GetUserMessageFunction();
				await CheckCommand(userMessage);

				ReturnFunction(await _provider.SendMessage(userMessage));
			}
		}

		private void OverflowHandlerAsync(List<IMessage> messagesHistory)
		{
			RunRemAgent();
		}

		private async Task<bool> CheckCommand(string message)
		{
			switch (message)
			{
				case "/rem":
					RunRemAgent();
					return true;

				default:
					return false;
			}
		}

		private async Task RunRemAgent() 
		{
			_remAgent.Update(_provider.GetDialog());
			await _remAgent.Run();
		}
	}
}
