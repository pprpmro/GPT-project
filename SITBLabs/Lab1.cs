using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using GPTTest.Providers.ChatGPT;
using HtmlAgilityPack;

namespace SITBLabs
{
    public static class Lab1
    {
        public static Regex squareBrackets = new Regex(@"\[\d+\]");
        public static Regex populationRegex = new Regex(@"Население\s+—\s+(\d+)\s+чел");
        public static Regex foundedYearRegex = new Regex(@"в\s+(\d{4})\s+году");
        public static Regex postIndexYearRegex = new Regex(@"Почтовый индекс\s+(\d{6})");

        public static void Run()
        {
            var url = "https://ru.wikipedia.org/wiki/%D0%9A%D1%83%D1%80%D0%BA%D0%B8%D0%BD%D0%BE_(%D0%A2%D1%83%D0%BB%D1%8C%D1%81%D0%BA%D0%B0%D1%8F_%D0%BE%D0%B1%D0%BB%D0%B0%D1%81%D1%82%D1%8C)";
            var html = Task.Run(() => GetHtmlFromUrlAsync(url)).Result;

            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            var pageText = ExtractTextFromBodyHtml(htmlDocument);


            pageText = squareBrackets.Replace(pageText, " ");
            Console.WriteLine(pageText);

            var population = FindPopulation(pageText);
            Console.WriteLine($"Население поселка: {population}");

            var foundedYear = FindFoundedYear(pageText);
            Console.WriteLine($"Поселение основано в: {foundedYear}");

            var foundedPostIndex = FindPostIndex(pageText);
            Console.WriteLine($"Почтовый индекс: {foundedPostIndex}");

            var gptDialog = new ChatGPTDialog();
            gptDialog.SetSystemPrompt(" ");
        }


        static async Task<string> GetHtmlFromUrlAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                try
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    return await response.Content.ReadAsStringAsync();

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при получении страницы: {ex.Message}");
                    return string.Empty;
                }
            }
        }

        static string ExtractTextFromBodyHtml(HtmlDocument htmlDocument)
        {
            var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//body");
            if (bodyNode != null)
            {
                return HttpUtility.HtmlDecode(bodyNode.InnerText);
            }
            else
            {
                return string.Empty;
            }
        }

        static string FindPopulation(string text)
        {
            return FindDataByRegex(text, populationRegex);
        }

        static string FindFoundedYear(string text)
        {
            return FindDataByRegex(text, foundedYearRegex);
        }

        static string FindPostIndex(string text)
        {
            return FindDataByRegex(text, postIndexYearRegex);
        }

        static string FindDataByRegex(string text, Regex regex)
        {
            var match = regex.Match(text);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return "Не найдено";
        }
    }
}
