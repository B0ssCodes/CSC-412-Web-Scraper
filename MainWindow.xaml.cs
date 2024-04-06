using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;

namespace CSC_412_Web_Scraper
{
    // Class that stores the name of each car and its price at each company
    public class CarData
    {
        public string CarName { get; set; }
        public string Company1Price { get; set; }
        public string Company2Price { get; set; }
        public string Company3Price { get; set; }
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
            ScrapingText.Visibility = Visibility.Visible;

            // Create a new scraper object
            var scraper = new Scraper();

            // Use a Task.Run to not block the UI thread and run the backend logic on a seperate thread
            Task.Run(() => scraper.StartScraping()).ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    // Handle Exceptions
                }
                else
                {
                    // Update the UI with the scraped data
                    UpdateCarData(scraper.scrapedItems);
                }
            }, TaskScheduler.FromCurrentSynchronizationContext()); // Ensure the continuation runs on the UI thread
        }

        public void UpdateCarData(List<Scraper.ScrapedItem> scrapedItems)
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
            }

            // When the data is updated, the loading bar and the scraping text become invisible, and the data grid becomes visible
            LoadingBar.Visibility = Visibility.Collapsed;
            ScrapingText.Visibility = Visibility.Collapsed;
            CarDataGrid.Visibility = Visibility.Visible;
        }
    }
}