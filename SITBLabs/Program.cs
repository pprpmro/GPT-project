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
            Lab1.SityWikiRun();
            //Task.Run(() => Lab1.SityNewsRun()).Wait();
        }
    }
}
