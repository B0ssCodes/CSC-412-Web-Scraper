using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSC_412_Web_Scraper
{
    public class Scraper
    {
        private HtmlWeb htmlWeb = new HtmlWeb();
        private static readonly HttpClient httpClient = new HttpClient();
        public ConcurrentBag<ScrapedItem> scrapedItems = new ConcurrentBag<ScrapedItem>();
        private int prog = 0;
        private async Task<HtmlDocument> LoadHtmlAsync(string url)
        {
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            return htmlDocument;
        }

        public async Task StartScraping(IProgress<ProgressReport> progress)
        {
            string url1 = "https://zoom-motors.netlify.app";
            string url2 = "https://driveway-deals.netlify.app";
            string url3 = "https://easy-auto.netlify.app";

            List<string> visitedUrls1 = new List<string>();
            List<string> visitedUrls2 = new List<string>();
            List<string> visitedUrls3 = new List<string>();

            await Task.WhenAll(
                Scrape(url1, null, visitedUrls1, progress),
                Scrape(url2, null, visitedUrls2, progress),
                Scrape(url3, null, visitedUrls3, progress)
            );
        }

        private async Task Scrape(string url, string? passedHref, List<string> visitedUrls, IProgress<ProgressReport> progress)
        {
            string baseUrl = url;
            HtmlDocument htmlDocument = await LoadHtmlAsync(url + passedHref);

            HtmlNodeCollection productTitles = htmlDocument.DocumentNode.SelectNodes("//h4");
            HtmlNodeCollection productPrices = htmlDocument.DocumentNode.SelectNodes("//h5");
            HtmlNodeCollection anchorTags = htmlDocument.DocumentNode.SelectNodes("//a");

            if (productTitles != null)
            {
                for (int i = 0; i < productTitles.Count; i++)
                {
                    string title = productTitles[i].InnerText;
                    string priceWithDollarSign = productPrices[i].InnerText;
                    string priceWithoutDollarSign = priceWithDollarSign.Replace("$", "");
                    int price = int.Parse(priceWithoutDollarSign);

                    string finalUrl = baseUrl + passedHref;
                    string domain = new Uri(baseUrl).Host;

                    ScrapedItem item = new ScrapedItem
                    {
                        Title = title,
                        Price = price,
                        Url = finalUrl,
                        Domain = domain
                    };

                    scrapedItems.Add(item);
                    prog++;
                    progress.Report(new ProgressReport { Progress = prog, Url = finalUrl });
                }
            }

            List<Task> tasks = new List<Task>();

            foreach (HtmlNode anchorTag in anchorTags)
            {
                string href = anchorTag.GetAttributeValue("href", string.Empty);
                if (visitedUrls.Contains(href) || href == "/")
                {
                    continue;
                }
                else
                {
                    visitedUrls.Add(href);
                    tasks.Add(Scrape(baseUrl, href, visitedUrls, progress));
                }
            }
            await Task.WhenAll(tasks);
        }

        public class ScrapedItem
        {
            public string Title { get; set; } = string.Empty;
            public int Price { get; set; }
            public string Url { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
        }

        public class ProgressReport
        {
            public int Progress { get; set; }
            public string Url { get; set; } = string.Empty;
        }
    }
}