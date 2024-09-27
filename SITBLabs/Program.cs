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
        }
    }
}
