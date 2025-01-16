using GPTProject.Core.Providers.YandexGPT;

namespace GPTProject.ConsoleUI
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var yandexGPT = new YandexGPTDialog();
            yandexGPT.SendMessage("привет, кто ты?");
            Console.ReadLine();
        }
    }
}
