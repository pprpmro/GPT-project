using GPTProject.Core.Providers.ChatGPT;

namespace GPTProject.Core
{
    public class ChatBotHelper
    {
        private readonly IChatDialog userDialog;
        private readonly IChatDialog classificationDialog;

        private readonly Dictionary<string, string> availableTypesAndFileNames;

        public ChatBotHelper(
            string availableTypesAndFileNamesString,
            string filePaths,
            string userPrompt,
            string classificationPrompt)
        {
            this.classificationDialog = new ChatGPTDialog();
            this.userDialog = new ChatGPTDialog();
            this.availableTypesAndFileNames = new Dictionary<string, string>();

            var rawAvailavleTypes = availableTypesAndFileNamesString.Split(';').Select(x => x.Trim()).ToList();

            foreach (var item in rawAvailavleTypes)
            {
                var typeAndFileNamePair = item.Split(':').Select(x => x.Trim()).ToArray();

                var type = typeAndFileNamePair[0];
                var fileName = typeAndFileNamePair[1];

                availableTypesAndFileNames.Add(type, fileName);
            }

            availableTypesAndFileNames.Add("Неопределено", String.Empty);
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

        #region Propmpts
        private string GetSystemPrompt(string source)
        {
            return
                $"Я хочу чтобы ты выступил в роле консультанта магазина по продаже рукодельных ножей определенным вопросом, ты не должен отвечать на все подряд" +
                $" Дальше будет представлена твоя база знаний: \"{source}\"." +
                $" Для ответов пользователям используй только ее, не выдумывай ничего лишнего, если тебе не хватает знаний ответить на вопрос, скажи: \"Мне не хватает знаний ответить на это\"" +
                $" Если пользователь задал вопрос не по теме, то отвечай что это не входит в твои обязанности.";
        }

        private string GetClassificationPrompt()
        {
            return
                "Ты выступаешь в роли классификатора, котрый будет определять тип сообщения от пользователя на основе текущего и предыдущего ответов" +
                $" Твой ответ должен содержать только одну цифру" +
                $" Если пользователь спрашивает о том, как или чем заточить ножи, напиши 2" +
                $" Если пользователь спрашивает о том, как обращаться, ухаживать, мыть, беречь нож, напиши 1" +
                $" Если пользователь спрашивает о том какие товары (ножи) есть в каталоге или просит рассказать о конкретной модели, напиши 0" +
                $" В любом другом случае напиши 3" +
                $" Не выходи за рамки этих ответов, если пользователь спрашивает что-то выходящее за рамки предметной области, напиши 3";
        }
        #endregion
    }
}
