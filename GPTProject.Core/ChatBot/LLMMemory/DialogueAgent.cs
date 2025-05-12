using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs;
using GPTProject.Providers.Dialogs.Implementations;
using GPTProject.Providers.Dialogs.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;
		private readonly string _collectionName;
		private readonly MemoryAgent _memoryAgent;

		private string? _memories;

		public DialogueAgent(Dictionary<DialogType, ProviderType> providerTypes, string collectionName, VectorizerRequest request, string SystemPrompt = "") {
			_provider = DialogSelector.GetDialog(providerTypes, DialogType.User);
			_collectionName = collectionName;
			_memoryAgent = new MemoryAgent(providerTypes, _collectionName, request);

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

				await _provider.SendMessage(PromptManager.GetThisIsWhatYouRememberPrompt(_memories, userMessage), ReturnFunction);
			}
		}

		private void OverflowHandlerAsync(List<IMessage> messagesHistory)
		{
			Task.Run(async () =>
			{
				try
				{
					await _memoryAgent.Save(_provider.GetDialog());
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex.Message);
				}
			});
		}

		private async Task<bool> CheckCommand(string message)
		{
			switch (message)
			{
				case "/save":
					_ = Task.Run(async () =>
					{
						try
						{
							await _memoryAgent.Save(_provider.GetDialog());
						}
						catch (Exception ex)
						{
							Console.WriteLine(ex.Message);
						}
					});
					return true;

				default:
					return false;
			}
		}
	}
}
