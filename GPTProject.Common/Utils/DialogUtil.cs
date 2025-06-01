using GPTProject.Providers.Data.Dialogs;
using GPTProject.Providers.Dialogs;
using GPTProject.Providers.Dialogs.Enumerations;
using GPTProject.Providers.Dialogs.Interfaces;
using GPTProject.Providers.Factories.Implementations;
using GPTProject.Providers.Factories.Interfaces;
using System.Text.RegularExpressions;

namespace GPTProject.Common.Utils
{
	public static class DialogUtil
	{
		private static readonly IDialogFactory dialogFactory = new DialogFactory();
		public static IChatDialog GetDialog(Dictionary<DialogType, ProviderType> providerTypes, DialogType dialogType)
		{
			if (!providerTypes.TryGetValue(dialogType, out var providerType))
			{
				throw new ArgumentException($"Поставщик для {dialogType} не задан в providerTypes.");
			}

			return dialogFactory.Create(providerType);
		}

		public static string CleanMessageFromPrompts(string message)
		{
			/*string cleaned = Regex.Replace(message,
				@"Вот список планов:\n.*?\nНЕ выписывай пользователю планы без запроса.*?Дата сегодня: .*?\n?",
				"",
				RegexOptions.Singleline);*/

			string cleaned = Regex.Replace(message,
				@"Предыдущая информация \(не обязательно использовать всю, бери только необходимое\): \n\[.*?\]\n",
				"",
				RegexOptions.Singleline);

			cleaned = Regex.Replace(cleaned, @"\n{3,}", "\n\n");

			return cleaned.Trim();
		}

		public static List<IMessage> CleanMessagesHistoryFromPrompts(List<IMessage> messagesHistory)
		{
			List<IMessage> cleanMessagesHistory = [];
			foreach (IMessage message in messagesHistory)
			{
				var cleanMessage = new Message
				{
					Role = message.Role
				};

				if (message.Role != DialogRole.Developer && message.Role != DialogRole.System)
				{
					cleanMessage.Content = CleanMessageFromPrompts(message.Content);
				}
				else
				{
					cleanMessage.Content = message.Content;
				}

				cleanMessagesHistory.Add(cleanMessage);
			}

			return cleanMessagesHistory;
		}
	}
}
