using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Linq;
using System.Diagnostics;
using System.Windows.Documents;
using static CSC_413_Web_Scraper.Scraper;
using System.Windows.Controls;
using System.Collections.Concurrent;
using OfficeOpenXml;
using System.IO;
using CsvHelper;
using System.Globalization;



namespace CSC_413_Web_Scraper
{
    // Class that stores the name of each car and its price at each company
    public class CarData
    {
        public string CarName { get; set; } = string.Empty;
        public string Company1Price { get; set; } = string.Empty;
        public string Company2Price { get; set; } = string.Empty;
        public string Company3Price { get; set; } = string.Empty;
        public string AveragePrice { get; set; } = string.Empty;
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
            CarCollection = [];
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
                var carData = CarCollection.AsParallel().FirstOrDefault(c => c.CarName == item.Title);
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

                // After setting the prices, compare them and set the RecommendedCompany based on the lowest price
                int price1 = int.TryParse(carData.Company1Price, out int tempVal1) ? tempVal1 : int.MaxValue;
                int price2 = int.TryParse(carData.Company2Price, out int tempVal2) ? tempVal2 : int.MaxValue;
                int price3 = int.TryParse(carData.Company3Price, out int tempVal3) ? tempVal3 : int.MaxValue;

                ConcurrentBag<int> bag = [price1, price2, price3];

                if (price1 <= price2 && price1 <= price3)
                {
                    carData.RecommendedCompany = "Buy From Zoom Motors: ";
                    carData.RecommendedUrl = scrapedItems.AsParallel().First(item => item.Domain == "zoom-motors.netlify.app" && item.Title == carData.CarName).Url;
                }
                else if (price2 <= price1 && price2 <= price3)
                {
                    carData.RecommendedCompany = "Buy From Driveway Deals: ";
                    carData.RecommendedUrl = scrapedItems.AsParallel().First(item => item.Domain == "driveway-deals.netlify.app" && item.Title == carData.CarName).Url;
                }
                else
                {
                    carData.RecommendedCompany = "Buy From Easy Auto: ";
                    carData.RecommendedUrl = scrapedItems.AsParallel().First(item => item.Domain == "easy-auto.netlify.app" && item.Title == carData.CarName).Url;
                }

                // Calculate the average price of the car at different companies using PLINQ
                ParallelQuery<int> averagePrice = bag.AsParallel().Where(price => price != int.MaxValue).Where(price => price != 0);
                carData.AveragePrice = averagePrice.Any() ? Math.Round(averagePrice.Average()).ToString() : "N/A";

            }

            // When the data is updated, the loading bar and the scraping text become invisible, and the data grid becomes visible
            LoadingBar.Visibility = Visibility.Collapsed;
            ScrapingText.Visibility = Visibility.Collapsed;
            LoadingBarNum.Visibility = Visibility.Collapsed;
            SearchBorder.Visibility = Visibility.Visible;
            IntroText.Visibility = Visibility.Collapsed;
            CarDataGrid.Visibility = Visibility.Visible;
            ExcelButton.Visibility = Visibility.Visible;
            CSVButton.Visibility = Visibility.Visible;
            
        }

        // Open the URL in the default browser when the hyperlink is clicked
        private void Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            var hyperlink = (Hyperlink)e.OriginalSource;
            Process.Start(new ProcessStartInfo(hyperlink.NavigateUri.AbsoluteUri) { UseShellExecute = true });
        }

        // Uses the EPPlus package to write the data to an Excel file and saves it to the desktop.
        private void ExcelButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "CarData"; // Default file name
            dialog.DefaultExt = ".xlsx"; // Default file extension
            dialog.Filter = "Excel documents (.xlsx)|*.xlsx"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dialog.FileName;

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                using (ExcelPackage pck = new ExcelPackage())
                {
                    //Create the worksheet
                    ExcelWorksheet ws = pck.Workbook.Worksheets.Add("CarData");

                    // Specify the column names
                    ws.Cells[1, 1].Value = "Car Name";
                    ws.Cells[1, 2].Value = "Dealer 1 Price";
                    ws.Cells[1, 3].Value = "Dealer 2 Price";
                    ws.Cells[1, 4].Value = "Dealer 3 Price";
                    ws.Cells[1, 5].Value = "Average Price";
                    ws.Cells[1, 6].Value = "Recommended Company";
                    ws.Cells[1, 7].Value = "Recommended URL";

                    // Loop over the data and add it to the Excel file
                    int rowStart = 2;
                    foreach (var item in CarCollection)
                    {
                        ws.Cells[rowStart, 1].Value = item.CarName;
                        ws.Cells[rowStart, 2].Value = item.Company1Price;
                        ws.Cells[rowStart, 3].Value = item.Company2Price;
                        ws.Cells[rowStart, 4].Value = item.Company3Price;
                        ws.Cells[rowStart, 5].Value = item.AveragePrice;
                        ws.Cells[rowStart, 6].Value = item.RecommendedCompany;
                        ws.Cells[rowStart, 7].Value = item.RecommendedUrl;
                        rowStart++;
                    }

                    // Save the new file to the selected path
                    using (FileStream fs = new FileStream(filename, FileMode.Create))
                    {
                        pck.SaveAs(fs);
                    }

                    // Display a message box to notify the user that the file has been saved
                    MessageBox.Show("Excel file saved successfully!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Uses the CSVHelper package to write the data to a CSV file and saves it to the desktop.
        private void CSVButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.FileName = "CarData"; // Default file name
            dialog.DefaultExt = ".csv"; // Default file extension
            dialog.Filter = "CSV documents (.csv)|*.csv"; // Filter files by extension

            // Show save file dialog box
            bool? result = dialog.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dialog.FileName;

                

                using (var writer = new StreamWriter(filename))
                using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
                {
                    csv.WriteRecords(CarCollection);
                }

                // Display a message box to notify the user that the file has been saved
                MessageBox.Show("CSV file saved successfully!", "Notification", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // This Search Box uses a PLINQ query to check if the car name contains the search text and if it does, it creeates a new ObservableCollection with the filtered items
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = SearchBox.Text.ToLower();
            var filteredItems = CarCollection.AsParallel().Where(car => car.CarName.ToLower().Contains(searchText));
            CarDataGrid.ItemsSource = new ObservableCollection<CarData>(filteredItems);
        }
    }

    
}