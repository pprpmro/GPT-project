using System.Text.RegularExpressions;
using System.Web;
using GPTTest;
using GPTTest.Providers.ChatGPT;
using HtmlAgilityPack;

namespace SITBLabs
{
    public class Program
    {

        static void Main(string[] args)
        {
            Task.Run(() => Lab1.Run()).Wait();

            //var helper = new ChatBotHelper();
            //await gptDialog.SendMessageByRole(Role.System, "");

            //var content = Console.ReadLine();
            //await gptDialog.SendMessageByRole(Role.System, content);


            //var gigaChatDialog = new GigaChatDialog();
            //await gigaChatDialog.GetCompletionsMessage(Role.System, "Я хочу, чтобы вы выступили в роли рекламодателя. \r\nВы создадите кампанию по продвижению товара или услуги по вашему выбору. \r\nВы выберете целевую аудиторию, разработаете ключевые сообщения и слоганы, \r\nвыберете медиаканалы для продвижения и решите, какие дополнительные действия необходимы для достижения ваших целей.\r\nПользователь предоставит вам информацию о товаре и его особенностях, целевой аудитории.\r\nЕсли информации, которую предоставил пользователь недостаточно, задай уточняющий вопрос.\r\nПример запроса: \"Мне нужна помощь в создании рекламной кампании \r\nдля нового вида энергетического напитка, ориентированного на молодых взрослых в возрасте 18-30 лет\".\r\nВ начале диалога первым предложением представся кто ты и в чем твое предназначение. Вторым предложением обратись с к пользователю, чтобы он предоставил необходимую информацию.\r\nНе выходи за рамки своей предметной области. Если пользователь задал вопрос не по теме, то отвечай \"Извините, это не входит в мои обязанности\".");

            //while (true)
            //{
            //    var content = Console.ReadLine();
            //    var result = helper.SendRequest(content);
            //    Console.WriteLine(result);
            //}
        }
    }
}
