using HtmlAgilityPack;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CSC_412_Web_Scraper
{
    public class Scraper
    {
        // Create a new HtmlWeb object to load HTML pages from a URL
        private HtmlWeb htmlWeb = new HtmlWeb();

        //This ConcurrentBag will hold all scraped items.
        public ConcurrentBag<ScrapedItem> scrapedItems = new ConcurrentBag<ScrapedItem>();

        // Method to start the scraping process
        public async Task StartScraping()
        {
            // The URLs of the 3 companies
            string url1 = "https://zoom-motors.netlify.app";
            string url2 = "https://driveway-deals.netlify.app";
            string url3 = "https://easy-auto.netlify.app";

            // Lists to keep track of visited URLs for each company
            List<string> visitedUrls1 = new List<string>();
            List<string> visitedUrls2 = new List<string>();
            List<string> visitedUrls3 = new List<string>();

            // Call the Scrape method in parallel
            await Task.WhenAll(
                Scrape(url1, null, visitedUrls1),
                Scrape(url2, null, visitedUrls2),
                Scrape(url3, null, visitedUrls3)
            );

        }

        // Takes the base URL, a nullable string for passed href (anything extra on the url), and the list of visited URLs created above
        //Recursively follows links on each page to scrape additional pages
        //url: Base URL to scrape
        //passedHref: Additional path to append to the base URL
        //visitedUrls: List of already visited URLs to avoid duplicates
        private async Task Scrape(string url, string? passedHref, List<string> visitedUrls)
        {
            // Base URL of the site
            string baseUrl = url;

            // Load the html document from the URL
            HtmlDocument htmlDocument = htmlWeb.Load(url + passedHref);

            // Select the h4 tags for titles and the h5 tags for prices
            HtmlNodeCollection productTitles = htmlDocument.DocumentNode.SelectNodes("//h4");
            HtmlNodeCollection productPrices = htmlDocument.DocumentNode.SelectNodes("//h5");

            // Select all the anchor tags
            HtmlNodeCollection anchorTags = htmlDocument.DocumentNode.SelectNodes("//a");
            
            // If we have titles, loop through them and create a new ScrapedItem object for each one
            if (productTitles != null)
            {
                for (int i = 0; i < productTitles.Count; i++)
                {
                    // Get the title and price of the product
                    string title = productTitles[i].InnerText;
                    string priceWithDollarSign = productPrices[i].InnerText;
                    string priceWithoutDollarSign = priceWithDollarSign.Replace("$", "");
                    int price = int.Parse(priceWithoutDollarSign);

                    // Get the final URL (Might add a URL for each car instead of the category)
                    string finalUrl = baseUrl + passedHref;

                    // Get the domain of the URL
                    string domain = new Uri(baseUrl).Host;

                    // Create a new ScrapedItem object and add it to the bag
                    ScrapedItem item = new ScrapedItem
                    {
                        Title = title,
                        Price = price,
                        Url = finalUrl,
                        Domain = domain
                    };

                    scrapedItems.Add(item);
                }
            }

            //this will hold all the tasks that will be created for each anchor tag 
            List<Task> tasks = new List<Task>();

            // Loop over each anchor tag and scrape the URL if it hasn't been visited yet
            // If the href is already in the visistedUrls list or if it's the root ("/"), skip it
            //Otherwise, add the href to the visistedUrls list and start a new scrape task for it
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

                    //Add the task to the list instead of awaiting it
                    tasks.Add(Scrape(baseUrl, href, visitedUrls));
                }
            }
            await Task.WhenAll(tasks);
        }

        // Class to hold the scraped data
        public class ScrapedItem
        {
            public string Title { get; set; } = string.Empty;
            public int Price { get; set; }
            public string Url { get; set; } = string.Empty;
            public string Domain { get; set; } = string.Empty;
        }


    }
}
