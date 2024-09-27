using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;

namespace SITBLabs
{
    public static class Lab1
    {
        public static Regex squareBrackets = new Regex(@"\[\d+\]");
        public static Regex populationRegex = new Regex(@"Население\s+—\s+(\d+)\s+чел");
        public static Regex foundedYearRegex = new Regex(@"в\s+(\d{4})\s+году");

        public static string tulaNewsUrl = "https://tulacity.gosuslugi.ru/dlya-zhiteley/novosti-i-reportazhi/?cur_cc=40&curPos=";

        public static object locker = new object();

        public static async Task Run()
        {
            var commonList = new List<string>();

            Parallel.For(0, 9, (i) =>
            {
                Console.WriteLine($"Beginning iteration {i}");

                var localResult = GetTagsByIndex((i * 10).ToString());

                lock(locker)
                {
                    commonList.AddRange(localResult);
                }

                Console.WriteLine($"Completed iteration {i}");
            });

            var result = commonList.GroupBy(x => x).Select(group => new { Tag = group.Key, Count = group.Count() }).OrderByDescending(x => x.Count).ToList();

            foreach (var item in result)
            {
                await Console.Out.WriteLineAsync($"{item.Tag} : {item.Count}");
            }
        }

        public static void KurkinoTestRun()
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

            string ExtractTextFromBodyHtml(HtmlDocument htmlDocument)
            {
                var bodyNode = htmlDocument.DocumentNode.SelectSingleNode("//*[@id=\"mw-content-text\"]");
                if (bodyNode != null)
                {
                    return HttpUtility.HtmlDecode(bodyNode.InnerText);
                }
                else
                {
                    return string.Empty;
                }
            }

            string FindPopulation(string text)
            {
                return FindDataByRegex(text, populationRegex);
            }

            string FindFoundedYear(string text)
            {
                return FindDataByRegex(text, foundedYearRegex);
            }
        }

        public static List<string> GetTagsByIndex(string startIndex = "0")
        {
            var url = tulaNewsUrl + startIndex;

            var html = Task.Run(() => GetHtmlFromUrlAsync(url)).Result;
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);

            if (htmlDocument is null)
            {
                return new List<string>();
            }

            return htmlDocument.DocumentNode
                .SelectNodes("//span[@class=\"item-category tpl-text-alt\"]")
                .Select(x => x.InnerText.Trim())
                .ToList();
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
