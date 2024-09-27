using GPTProject.Core;
using GPTTest.KnowledgeBase.Knifes;
using GPTTest.Providers.ChatGPT;

namespace GPTTest
{
    public class ChatBotHelper
    {
        private readonly IGPTDialog userDialog;
        private readonly IGPTDialog classificationDialog;

        public ChatBotHelper()
        {
            classificationDialog = new ChatGPTDialog();
            userDialog = new ChatGPTDialog();
        }

        private SourceType? lastType = null;

        public async Task<string> SendRequest(string userPrompt)
        {
            var sourceType = await GetSourceTypeByUserPrompt(userPrompt);
            await Console.Out.WriteLineAsync($"Type: {sourceType}");

            if (sourceType == SourceType.Неопределено)
            {
                return "Неизвестно";
            }

            var source = GetSourceBySourceType(sourceType);

            if (string.IsNullOrEmpty(source))
            {
                throw new Exception("Source was null");
            }


            if (!lastType.HasValue || lastType.Value != sourceType)
            {
                lastType = sourceType;
                userDialog.ClearDialog();
                userDialog.SetSystemPrompt(message: GetSystemPrompt(source));
            }

            return await userDialog.SendUserMessageAndGetFirstResult(userPrompt);
        }

        private async Task<SourceType> GetSourceTypeByUserPrompt(string userPrompt)
        {
            classificationDialog.SetSystemPrompt(message: GetClassificationPrompt());

            var result = await classificationDialog.SendUserMessageAndGetFirstResult(userPrompt);
            int.TryParse(result, out var resultSource);
            classificationDialog.ClearDialog();
            return (SourceType)resultSource;
        }

        #region Mapping
        private string GetPathByType(SourceType sourceType)
        {
            var startPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\"));
            switch (sourceType)
            {
                case SourceType.Каталог:
                    {
                        return Path.Combine(startPath, @"KnowledgeBase\Knifes\Sources\Katalog.txt");
                    }

                case SourceType.Как_затачивать:
                    {
                        return Path.Combine(startPath, @"KnowledgeBase\Knifes\Sources\HowToSharpenKnife.txt");
                    }

                case SourceType.Рекомендации_к_использованию:
                    {
                        return Path.Combine(startPath, @"KnowledgeBase\Knifes\Sources\HowToUse.txt");
                    }
            }

            throw new Exception("Incorrect type");
        }
        public string GetSourceBySourceType(SourceType sourceType)
        {
            var result = string.Empty;
            var path = GetPathByType(sourceType);
            using (var reader = new StreamReader(path))
            {
                result = reader.ReadToEnd();
            }
            return result;
        }
        #endregion

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
