using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
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

        CancellationTokenSource cancellationTokenSource = null;

        private async void Search_Click(object sender, RoutedEventArgs e)
        {
            #region Before loading stock data
            var watch = new Stopwatch();
            watch.Start();
            StockProgress.Visibility = Visibility.Visible;
            StockProgress.IsIndeterminate = true;
            Search.ToolTip = "Cancel";
            #endregion

            #region cancelation

            if (cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource = null;
                Search.ToolTip = "Search";
                return;
            }

            cancellationTokenSource = new CancellationTokenSource();

            #endregion cancelation

            try
            {
                var tickers = Ticker.Text.Split(',', ' ');
                var tickerloadingTasks = new List<Task<IEnumerable<StockPrice>>>();
                foreach (var ticker in tickers)
                {
                    var loadData = GetStocks(ticker, cancellationTokenSource.Token);
                    tickerloadingTasks.Add(loadData);
                }

                var timeOutTask = Task.Delay(2000);
                var allStocks = Task.WhenAll(tickerloadingTasks);
                var completed = await Task.WhenAny(timeOutTask, allStocks);

                if (completed == timeOutTask)
                {
                    cancellationTokenSource.Cancel();
                    cancellationTokenSource = null;
                    throw new Exception("Timeout");
                }

                Stocks.ItemsSource = allStocks.Result.SelectMany(sr => sr);
            }
            catch (Exception)
            {

                throw;
            }

            #region comment

            //cancellationTokenSource.Token.Register(() =>
            //{
            //    Notes.Text += "Cancelation Token";
            //});

            //loadAllLines.ContinueWith(t =>
            //{
            //    Dispatcher.Invoke(() =>
            //    {
            //        Notes.Text += $"{t.Exception.InnerException.Message}";
            //        StockProgress.Visibility = Visibility.Hidden;
            //    });
            //}, TaskContinuationOptions.OnlyOnFaulted);

            //var processStockTasks = loadAllLines.ContinueWith(t =>
            //{
            //    CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            //    var data = new List<StockPrice>();
            //    var lines = t.Result;
            //    foreach (var line in lines.Skip(1))
            //    {
            //        var segments = line.Split(',');
            //        DateTime dateTime10 = DateTime.Now;

            //        for (var i = 0; i < segments.Length; i++) segments[i] = segments[i].Trim('\'', '"');
            //        bool isSuccess = DateTime.TryParse(segments[1], out dateTime10);
            //        var price = new StockPrice
            //        {
            //            Ticker = segments[0],
            //            TradeDate = isSuccess ? dateTime10 : DateTime.Now,
            //            Volume = Convert.ToInt32(segments[6]),
            //            Change = Convert.ToDouble(segments[7], cultureInfo),
            //            ChangePercent = Convert.ToDouble(segments[8], cultureInfo),
            //        };
            //        data.Add(price);
            //    }

            //    return data;
            //}, continuationOptions: TaskContinuationOptions.OnlyOnRanToCompletion);

           
            //processStockTasks.ContinueWith(t =>
            //{
            //    var data = t.Result;
            //    Dispatcher.Invoke(() =>
            //    {
            //        Stocks.ItemsSource = data.Where(price => price.Ticker == Ticker.Text);
            //    });
            //}, continuationOptions: TaskContinuationOptions.OnlyOnRanToCompletion);

            #endregion comment

            #region After stock data is loaded
            StocksStatus.Text = $"Loaded stocks for {Ticker.Text} in {watch.ElapsedMilliseconds}ms";
            StockProgress.Visibility = Visibility.Hidden;
            #endregion
        }

        private Task<List<string>> SearchForStoch(CancellationToken cancellationToken)
        {
            var loadLines = Task.Run(async () =>
            {
                var lines = new List<string>();

                using (var stream = new StreamReader(File.OpenRead(@"D:\Demo\StockPrices.csv")))
                {
                    string line;
                    while ((line = await stream.ReadLineAsync()) != null)
                    {
                        if(cancellationToken.IsCancellationRequested)
                        {
                            return lines;
                        }
                        lines.Add(line);
                    }
                }

                return lines;
            }, cancellationToken);

            //var lines = File.ReadAllLines(@"D:\Demo\StockPrices.csv");
                //return lines;
            //}, cancellationToken);
            return loadLines;
        }

        public async Task<IEnumerable<StockPrice>> GetStocks(string text, CancellationToken cancellationToken)
        {
            using (var client = new HttpClient())
            {
                var httpResponse = await client.GetAsync($"http://localhost:61363/api/stocks/{text}");

                    httpResponse.EnsureSuccessStatusCode();

                    var content = await httpResponse.Content.ReadAsStringAsync();

                    var data = JsonConvert.DeserializeObject<IEnumerable<StockPrice>>(content);

                    return data;              
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
