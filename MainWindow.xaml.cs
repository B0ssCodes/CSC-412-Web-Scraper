using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using System.Windows.Documents;
using static CSC_412_Web_Scraper.Scraper;

namespace CSC_412_Web_Scraper
{
    // Class that stores the name of each car and its price at each company
    public class CarData
    {
        public string CarName { get; set; } = string.Empty;
        public string Company1Price { get; set; } = string.Empty;
        public string Company2Price { get; set; } = string.Empty;
        public string Company3Price { get; set; } = string.Empty;
        public string RecommendedCompany { get; set; } = string.Empty;
        public string RecommendedUrl { get; set; } = string.Empty;
    }

    public partial class MainWindow : Window
    {
        // The whole car collection to display in a table in the end
        public ObservableCollection<CarData> CarCollection { get; set; }

        // XAML main window constructor
        public MainWindow()
        {
            InitializeComponent();
            CarCollection = new ObservableCollection<CarData>();
            CarDataGrid.ItemsSource = CarCollection;

            // Set the data context of the window to itself
            this.DataContext = this;
        }

        // When button is clicked, start the scraping process
        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            // Change the visibility of the UI elements
            StartButton.Visibility = Visibility.Collapsed;
            LoadingBar.Visibility = Visibility.Visible;
            LoadingBarNum.Visibility = Visibility.Visible;
            ScrapingText.Visibility = Visibility.Visible;
            UrlTextBlock.Visibility = Visibility.Visible;

            // Create a new scraper object
            var scraper = new Scraper();

            // Create a Progress<int> and handle its ProgressChanged event to update the UI
            var progress = new Progress<ProgressReport>(report =>
            {
                // Update the loading bar and the URL
                LoadingBar.Value = report.Progress;
                UrlTextBlock.Text = report.Url;  // Assuming you have a TextBlock named UrlTextBlock
            });

            // Start the stopwatch
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            // Use a Task.Run to not block the UI thread and run the backend logic on a separate thread
            Task.Run(() => scraper.StartScraping(progress)).ContinueWith(t =>
            {
                stopwatch.Stop();
                if (t.IsFaulted)
                {
                    // Handle Exceptions
                    Debug.WriteLine($"Exception: {t.Exception}");
                }
                else
                {
                    // Update the UI with the scraped data
                    UpdateCarData(scraper.scrapedItems);

                    // Display the elapsed time
                    TimeSpan ts = stopwatch.Elapsed;
                    string elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:00}",
                        ts.Hours, ts.Minutes, ts.Seconds,
                        ts.Milliseconds / 10);
                    ElapsedTimeBorder.Visibility = Visibility.Visible;
                    ElapsedTimeTextBlock.Text = "Runtime " + elapsedTime;
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure the continuation runs on the UI thread
        }



        public void UpdateCarData(IEnumerable<Scraper.ScrapedItem> scrapedItems)
        {
            // Clear the existing data
            CarCollection.Clear();

            // Add the new data
            foreach (var item in scrapedItems)
            {
                // Find the car in the collection, if it doesn't exist, create a new one
                var carData = CarCollection.FirstOrDefault(c => c.CarName == item.Title);
                if (carData == null)
                {
                    carData = new CarData { CarName = item.Title };
                    CarCollection.Add(carData);
                }

                // Switch to know for which company the car price belongs
                switch (item.Domain)
                {
                    case "zoom-motors.netlify.app":
                        carData.Company1Price = item.Price.ToString();
                        break;
                    case "driveway-deals.netlify.app":
                        carData.Company2Price = item.Price.ToString();
                        break;
                    case "easy-auto.netlify.app":
                        carData.Company3Price = item.Price.ToString();
                        break;
                }

                // After setting the prices, compare them and set the RecommendedCompany
                int price1 = int.TryParse(carData.Company1Price, out int tempVal1) ? tempVal1 : int.MaxValue;
                int price2 = int.TryParse(carData.Company2Price, out int tempVal2) ? tempVal2 : int.MaxValue;
                int price3 = int.TryParse(carData.Company3Price, out int tempVal3) ? tempVal3 : int.MaxValue;

                if (price1 <= price2 && price1 <= price3)
                {
                    carData.RecommendedCompany = "Buy From Zoom Motors: ";
                    carData.RecommendedUrl = scrapedItems.First(item => item.Domain == "zoom-motors.netlify.app" && item.Title == carData.CarName).Url;
                }
                else if (price2 <= price1 && price2 <= price3)
                {
                    carData.RecommendedCompany = "driveway-deals.netlify.app";
                    carData.RecommendedUrl = scrapedItems.First(item => item.Domain == "driveway-deals.netlify.app" && item.Title == carData.CarName).Url;
                }
                else
                {
                    carData.RecommendedCompany = "easy-auto.netlify.app";
                    carData.RecommendedUrl = scrapedItems.First(item => item.Domain == "easy-auto.netlify.app" && item.Title == carData.CarName).Url;
                }
            }

            // When the data is updated, the loading bar and the scraping text become invisible, and the data grid becomes visible
            LoadingBar.Visibility = Visibility.Collapsed;
            ScrapingText.Visibility = Visibility.Collapsed;
            CarDataGrid.Visibility = Visibility.Visible;
        }

        // Open the URL in the default browser when the hyperlink is clicked
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var hyperlink = (Hyperlink)e.OriginalSource;
            Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
        }

    }

    
}