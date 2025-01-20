using GPTProject.Core.Providers.ChatGPT;
using GPTProject.Core.Providers.GigaChat;
using GPTProject.Core.Providers.YandexGPT;

namespace GPTProject.Core
{
    public class ChatBotHelper
    {
        private readonly IChatDialog userDialog;
        private readonly IChatDialog classificationDialog;
        private readonly IChatDialog cleansingDialog;
        private readonly IChatDialog clarifyingDialog;
        private readonly IChatDialog responseGeneratingDialog;

        private readonly Dictionary<string, string> availableTypesAndFileNames;

        public ChatBotHelper(
            Type providerType,
            List<string> filePaths)
        {
            this.classificationDialog = GetChatDialogProvider(providerType);
            this.userDialog = GetChatDialogProvider(providerType);
            this.cleansingDialog = GetChatDialogProvider(providerType);
            this.clarifyingDialog = GetChatDialogProvider(providerType);
            this.responseGeneratingDialog = GetChatDialogProvider(providerType);

            availableTypesAndFileNames = GetAvailableTypesAndFileNames(filePaths);
        }

        private string? lastType = null;

        public async Task<string> GetResultByPrompt(string userPrompt)
        {
            var sourceType = await GetSourceTypeByUserPrompt(userPrompt);

            if (sourceType == "Неизвестно")
            {
                return "Неизвестно";
            }

            //var source = GetSourceBySourceType(sourceType);

            //if (string.IsNullOrEmpty(source))
            //{
            //    throw new Exception("Source was null");
            //}


            //if (!lastType.HasValue || lastType.Value != sourceType)
            //{
            //    lastType = sourceType;
            //    userDialog.ClearDialog();
            //    userDialog.SetSystemPrompt(message: GetSystemPrompt(source));
            //}

            return await userDialog.SendMessage(userPrompt);
        }

        private async Task<string> GetSourceTypeByUserPrompt(string userPrompt)
        {
            classificationDialog.SetSystemPrompt(message: GetClassificationPrompt());

            var result = await classificationDialog.SendMessage(userPrompt);
            int.TryParse(result, out var resultSource);
            classificationDialog.ClearDialog();

            return "";
            //return (SourceType)resultSource;
        }

        private Dictionary<string,string> GetAvailableTypesAndFileNames(List<string> filePaths)
        {
            var availableTypesAndFileNames = new Dictionary<string,string>();
            foreach (var filePath in filePaths)
            {
                if (File.Exists(filePath))
                {
                    string fileName = Path.GetFileName(filePath);
                    availableTypesAndFileNames[fileName] = filePath;
                }
                else
                {
                    Console.WriteLine($"Can't find: {filePath}");
                }
            }

            if (availableTypesAndFileNames.Count == 0)
            {
                throw new Exception("Files don't exist");
            }

            availableTypesAndFileNames.Add("Неопределено", String.Empty);


            return availableTypesAndFileNames;
        }
        private IChatDialog GetChatDialogProvider(Type type)
        {
            switch (type)
            {
                case Type.ChatGPT:
                    return new ChatGPTDialog();
                case Type.YandexGPT:
                    return new YandexGPTDialog();
                case Type.GigaChat:
                    return new GigaChatDialog();
                default:
                    throw new NotImplementedException();
            }
        }

        #region Propmpts
        private string GetSystemPrompt(string source)
        {
            return
                $"Я хочу чтобы ты выступил в роле консультанта магазина по продаже рукодельных ножей определенным вопросом, ты не должен отвечать на все подряд" +
                $" Для ответов пользователям используй только предоставленную базу знаний, не выдумывай ничего лишнего, если тебе не хватает знаний ответить на вопрос, скажи: \"Мне не хватает знаний ответить на это\"" +
                $" Если пользователь задал вопрос не по теме, то отвечай что это не входит в твои обязанности." +
                $" Твоя база знаний:{Environment.NewLine}\"{source}\"";
        }

        private string GetClassificationPrompt(List<string> types)
        {
            var index = 0;
            var classificationPrompt =
                "Ты выступаешь в роли классификатора, котрый будет определять тип сообщения от пользователя на основе текущего и предыдущего ответов" +
                $" Твой ответ должен содержать только одну цифру" +
                $" Не выходи за рамки этих ответов, если пользователь спрашивает что-то выходящее за рамки предметной области, напиши -1";

            foreach (var type in types)
            {
                var typeDescription = $"{Environment.NewLine}Если пользовательский запрос относится к теме {type}, напиши {index}";

                classificationPrompt += typeDescription;
                index++;
            }
            return classificationPrompt;
        }

        private string GetClarifyingPrompt()
        {
            return "Если предоставленных пользователем данных недостаточно для выполнения запроса, или если запрос имеет неоднозначности, ты должен задать уточняющий вопрос, чтобы уточнить детали или запросить недостающую информацию." +
                " Не пытайся угадывать или делать предположения без достаточных оснований. Твои уточняющие вопросы должны быть:" +
                "\r\nКраткими и ясными." +
                "\r\nНацеленными на получение информации, необходимой для точного выполнения запроса." +
                "\r\nУместными и по существу, исходя из предоставленных данных.";
        }

        private string GetResponseGeneratingPrompt()
        {
            return "Тебе предоставлен список ответов на разные вопросы. Твоя задача:" +
                "\r\nОбъединить все ответы в единый текст без повторений." +
                "\r\nСоставить ответ так, чтобы он был сжатым, логичным и емким." +
                "\r\nНикакую информацию из предоставленных ответов нельзя пропускать или выкидывать." +
                "\r\nНе добавляй информацию от себя, не придумывай и не интерпретируй данные иначе, чем они представлены." +
                "\r\nСохраняй последовательность и структуру, делая текст максимально понятным и компактным для читателя.";
        }
        #endregion
    }
}
