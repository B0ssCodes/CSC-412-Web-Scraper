using HtmlAgilityPack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace CSC_413_Web_Scraper
{
    // The Scraper class that will be used to scrape the websites
    public class Scraper
    {
        // The HtmlWeb object to load the HTML from the website
        private HtmlWeb htmlWeb = new HtmlWeb();

        // The HttpClient object to make the HTTP requests
        private static readonly HttpClient httpClient = new HttpClient();

        // The ConcurrentBag used to store the scraped items concurrently
        public ConcurrentBag<ScrapedItem> scrapedItems = new ConcurrentBag<ScrapedItem>();

        // The progress variable to keep track of the progress
        private int prog = 0;

        // The LoadHtmlAsync method to load the HTML from the website asynchronously
        private async Task<HtmlDocument> LoadHtmlAsync(string url)
        {
            var html = await httpClient.GetStringAsync(url);
            var htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(html);
            return htmlDocument;
        }

        // The StartScraping method to start the scraping process
        public async Task StartScraping(IProgress<ProgressReport> progress)
        {
            // The URLs of the websites to scrape

            string url1 = "https://zoom-motors.netlify.app";
            string url2 = "https://driveway-deals.netlify.app";
            string url3 = "https://easy-auto.netlify.app";

            // The lists to store the visited URLs for each site
            List<string> visitedUrls1 = new List<string>();
            List<string> visitedUrls2 = new List<string>();
            List<string> visitedUrls3 = new List<string>();

            // Start the scraping process for each website concurrently
            await Task.WhenAll(
                Scrape(url1, null, visitedUrls1, progress),
                Scrape(url2, null, visitedUrls2, progress),
                Scrape(url3, null, visitedUrls3, progress)
            );
        }

        // The Scrape method to scrape the websites, takes a URL, a passed href, a list of visited URLs, and a progress report
        private async Task Scrape(string url, string? passedHref, List<string> visitedUrls, IProgress<ProgressReport> progress)
        {
            // The base URL of the website
            string baseUrl = url;

            // Load the HTML from the website
            HtmlDocument htmlDocument = await LoadHtmlAsync(url + passedHref);

            // Select the product titles (h4), prices (h5), and anchor tags (a) from the HTML
            HtmlNodeCollection productTitles = htmlDocument.DocumentNode.SelectNodes("//h4");
            HtmlNodeCollection productPrices = htmlDocument.DocumentNode.SelectNodes("//h5");
            HtmlNodeCollection anchorTags = htmlDocument.DocumentNode.SelectNodes("//a");

            // If the product titles are not null, iterate through them
            if (productTitles != null)
            {
                for (int i = 0; i < productTitles.Count; i++)
                {
                    // Get the title, price, and URL of the product
                    string title = productTitles[i].InnerText;
                    string priceWithDollarSign = productPrices[i].InnerText;
                    string priceWithoutDollarSign = priceWithDollarSign.Replace("$", "");
                    int price = int.Parse(priceWithoutDollarSign);

                    string finalUrl = baseUrl + passedHref;
                    string domain = new Uri(baseUrl).Host;

                    // Create a new ScrapedItem object with the title, price, URL, and domain
                    ScrapedItem item = new ScrapedItem
                    {
                        Title = title,
                        Price = price,
                        Url = finalUrl,
                        Domain = domain
                    };

                    // Add the item to the scrapedItems list
                    scrapedItems.Add(item);

                    // Increment the progress and report it
                    prog++;
                    progress.Report(new ProgressReport { Progress = prog, Url = finalUrl });
                }
            }

            // If the anchor tags are not null, scrape the URLs concurrently
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

        // The ScrapedItem class to store the scraped items
        public class ScrapedItem
        {
            public string Title { get; set; } = string.Empty;
            public int Price { get; set; }
            public string Url { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
        }

        // The ProgressReport class to store the progress
        public class ProgressReport
        {
            public int Progress { get; set; }
            public string Url { get; set; } = string.Empty;
        }
    }
}