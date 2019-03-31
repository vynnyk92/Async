using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Navigation;
using Newtonsoft.Json;
using StockAnalyzer.Core.Domain;

namespace StockAnalyzer.Windows
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        
        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            #region Before loading stock data
            var watch = new Stopwatch();
            watch.Start();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
            #endregion
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;

            var loadAllLines = await Task.Run(() =>
            {
                var lines = File.ReadAllLines(@"D:\Demo\StockPrices.csv");
                return lines;
            });

            var data = new List<StockPrice>();

            foreach (var line in lines.Skip(1))
            {
                var segments = line.Split(',');
                DateTime dateTime10 = DateTime.Now;

                for (var i = 0; i < segments.Length; i++) segments[i] = segments[i].Trim('\'', '"');
                bool isSuccess = DateTime.TryParse(segments[1], out dateTime10);
                var price = new StockPrice
                {
                    Ticker = segments[0],
                    TradeDate = isSuccess ? dateTime10 : DateTime.Now,
                    Volume = Convert.ToInt32(segments[6]),
                    Change = Convert.ToDouble(segments[7], cultureInfo),
                    ChangePercent = Convert.ToDouble(segments[8], cultureInfo),
                };
                data.Add(price);
            }



            Dispatcher.Invoke(() =>
            {
                Stocks.ItemsSource = data.Where(price => price.Ticker == Ticker.Text);

            });


            #region After stock data is loaded
            StocksStatus.Text = $"Loaded stocks for {Ticker.Text} in {watch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
            #endregion
        }

        public async Task GetStocks()
        {
            using (var client = new HttpClient())
            {
                var httpResponse = await client.GetAsync($"http://localhost:61363/api/stocks/{Ticker.Text}");

                try
                {
                    httpResponse.EnsureSuccessStatusCode();

                    var content = await httpResponse.Content.ReadAsStringAsync();

                    var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);

                    Stocks.ItemsSource = data;

                }
                catch (Exception ex)
                {
                    Notes.Text += ex.Message;
                }
            }
        }

        private void Hyperlink_OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));

            e.Handled = true;
        }

        private void Close_OnClick(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
