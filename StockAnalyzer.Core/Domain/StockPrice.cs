using System;

namespace StockAnalyzer.Core.Domain
{
    public class StockPrice
    {
        public string Ticker  { get; set; }
        public DateTime TradeDate  { get; set; }
        public decimal? Open  { get; set; }
        public decimal? High  { get; set; }
        public decimal? Low  { get; set; }
        public decimal? Close  { get; set; }
        public int Volume  { get; set; }
        public double Change  { get; set; }
        public double ChangePercent { get; set; }
    }
}