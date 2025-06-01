using GPTProject.Common;
using GPTProject.Common.Utils;
using GPTProject.Core.Data;
using GPTProject.Core.Services.Implementations;
using GPTProject.Core.Services.Interfaces;
using GPTProject.Providers.Data.Dialogs;
using GPTProject.Providers.Data.Vectorizers;
using GPTProject.Providers.Dialogs;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Vectorizers.Interfaces;

namespace GPTProject.Core.ChatBot.LLMMemory
{
	public class DialogueAgent
	{
		private readonly IChatDialog _provider;
		private static readonly ICharacterService _characterService = new CharacterService();
		private readonly string _collectionName;
		private readonly MemoryAgent _memoryAgent;
		private readonly PlanningAgent _planningAgent;
		private readonly SummaryAgent _summaryAgent;

		private string? _memories;
		private Character _character;

		public DialogueAgent(Dictionary<DialogType, ProviderType> providerTypes, string collectionName, VectorizerRequest request, IVectorizer vectorizer, Character character, string SystemPrompt = "") {
			_provider = DialogUtil.GetDialog(providerTypes, DialogType.User);
			_collectionName = collectionName;
			_memoryAgent = new MemoryAgent(providerTypes, _collectionName, request, vectorizer);
			_planningAgent = new PlanningAgent(providerTypes);
			_summaryAgent = new SummaryAgent(providerTypes);

			_provider.SetOverflowHandler(OverflowHandlerAsync);
			_character = character;

			if (SystemPrompt != "") _provider.UpdateSystemPrompt(SystemPrompt);

			SetFirstMessage();
		}

		public async Task Run(Func<Task<string>> GetUserMessageFunction, Action<string?> ReturnFunction)
		{
			while (true)
			{
				_provider.SetCustomDialog(DialogUtil.CleanMessagesHistoryFromPrompts(_provider.GetDialog()));

				var userMessage = await GetUserMessageFunction();
				if(await CheckCommand(userMessage)) { continue; }

				_memories = await _memoryAgent.Restore(userMessage);

				string result = string.Empty;

				result += PromptManager.GetThisIsWhatYouRememberPrompt(_memories, userMessage);

				await _provider.SendMessage(result, ReturnFunction);
			}
		}

		public async Task Save()
		{
			await _memoryAgent.Save(_provider.GetDialog());

			if (_character.Plans is not null && _character.Plans != "NONE") 
			{
				_character.Plans = await _planningAgent.RemakeOldPlan(_provider.GetDialog(), _character.Plans);
			}
			else
			{
				_character.Plans = await _planningAgent.MakeNewPlan(_provider.GetDialog());
			}

			_character.Summary = await _summaryAgent.GetSummaryForContext(_provider.GetDialog());

			_characterService.SaveToJson(_character); //перенести
		}

		private void OverflowHandlerAsync(List<IMessage> messagesHistory)
		{
			Task.Run(async () =>
			{
				try
				{
					await Save();
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
							await Save();
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

		private void SetFirstMessage()
		{
			var dialog = _provider.GetDialog();
			var message = new Message
			{
				Role = DialogRole.User
			};

			string result = string.Empty;

			if (_character.Summary is not null)
			{
				result += PromptManager.GetThisIsWhatYouSummarizedPrompt(_character.Summary);
			}

			if (_character.Plans is not null && _character.Plans != "NONE")
			{
				result += PromptManager.GetThisIsWhatYouPlannedPrompt(_character.Plans);
			}

			if (result.Length > 0)
			{
				message.Content = result;

				dialog.Add(message);

				_provider.SetCustomDialog(dialog);
			}
		}
	}
}
