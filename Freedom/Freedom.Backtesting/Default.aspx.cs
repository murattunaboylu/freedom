using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace Freedom.Backtesting
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!Page.IsPostBack)
            {
                StartDateTextBox.Text = DateTime.Now.AddDays(-7).ToString("yyyyMMdd");
                EndDateTextBox.Text = DateTime.Now.ToString("yyyyMMdd");
            }
        }

        public void Simulate(DateTime start, DateTime end, int interval)
        {
            //Clear           
            OrdersListBox.Items.Clear();
            PnLLabel.Text = "0";

            //https://msdn.microsoft.com/en-us/library/hh297114%28v=vs.100%29.aspx?f=255&MSPPError=-2147217396
            var chart = new Chart();
            chart.ImageLocation = "~/TempImages/ChartPic_#SEQ(300,3)";
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            chart.Width = 800;
            var series = new Series() { ChartType = SeriesChartType.Candlestick };
            chart.Series.Add(series);

            //Read the OHLC from database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "ff-marketdata.database.windows.net";
            builder.UserID = "marketdata";
            builder.Password = "mar20X/b";
            builder.InitialCatalog = "marketdata";

            List<OHLC> ohlcList = new List<OHLC>();

            try
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("", connection))
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT o.Id, o.[Open], o.High, o.Low, o.[Close], Start FROM dbo.OHLC o " +
                            "WHERE o.[Start] >= @start AND o.[End] < @end ORDER BY o.Id ASC";
                        command.Parameters.Add(new SqlParameter("start", start));
                        command.Parameters.Add(new SqlParameter("end", end));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                var ohlc = new OHLC();

                                ohlc.Open = reader.GetDecimal(1);
                                ohlc.High = reader.GetDecimal(2);
                                ohlc.Low = reader.GetDecimal(3);
                                ohlc.Close = reader.GetDecimal(4);
                                ohlc.Start = reader.GetDateTime(5);

                                ohlcList.Add(ohlc);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Set the trading state
            State = TradingState.Initial;
            Orders = new List<Order>();

            //l h o c

            //Find the first trade
            //Find all trades within the next 5 minutes
            //If there are any trade
            //Calculate low, high, open, close
            if (ohlcList.Any())
            {
                start = ohlcList.First().Start > start ? ohlcList.First().Start : start;

                for (DateTime i = start; i < end; i = i.AddMinutes(interval))
                {
                    var ohlcInTheSameWindow = ohlcList.Where(o => o.Start >= i && o.Start < i.AddMinutes(interval));

                    if (ohlcInTheSameWindow.Any())
                    {
                        var low = ohlcInTheSameWindow.Select(t => t.Low).Min();
                        var high = ohlcInTheSameWindow.Select(t => t.High).Max();
                        var open = ohlcInTheSameWindow.First().Open;
                        var close = ohlcInTheSameWindow.Last().Close;

                        var ohlc = new OHLC(open, high, low, close);

                        //Check for indicators and make trading decisions
                        RelativeStrengthIndexStrategy(ohlc, ohlcInTheSameWindow.Last().Start);

                        DataPoints.Insert(0, ohlc);

                        series.Points.AddY(low, high, open, close);
                    }
                }
            }

            //Stats after simulation
            var stats = new Stats(Orders);

            PnLLabel.Text = $"Actual ({stats.Pnl.ToString()}) vs Market ({DataPoints.First().Close - DataPoints.Last().Open}) vs Target ({(end-start).TotalDays * 80})";
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Win ratio : {stats.WinRatio:N0}% <br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"# t-pairs : {stats.TradePairCount}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Max win   : {stats.MaxWin}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Max loss  : {stats.MaxLoss}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Mean win  : {stats.MeanWin:N2}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Mean loss : {stats.MeanLoss:N2}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Max hold  : {stats.MaxHoldInHours}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Mean hold : {stats.MeanHoldInHours:N2}<br/>" });
            StatsPlaceHolder.Controls.Add(new Label() { Text = $"Min hold  : {stats.MinHoldInHours}<br/>" });

            ChartPlaceHolder.Controls.Add(chart);
        }


        /// <summary>
        /// Leaving here for future development
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="interval"></param>
        public void RealTime(DateTime start, DateTime end, int interval)
        {
            //Clear           
            OrdersListBox.Items.Clear();
            PnLLabel.Text = "0";

            //https://msdn.microsoft.com/en-us/library/hh297114%28v=vs.100%29.aspx?f=255&MSPPError=-2147217396
            var chart = new Chart();
            chart.ImageLocation = "~/TempImages/ChartPic_#SEQ(300,3)";
            chart.ChartAreas.Add(new ChartArea("MainArea"));
            chart.Width = 800;
            var series = new Series() { ChartType = SeriesChartType.Candlestick };
            chart.Series.Add(series);

            //Read the trades from database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "ff-marketdata.database.windows.net";
            builder.UserID = "marketdata";
            builder.Password = "mar20X/b";
            builder.InitialCatalog = "marketdata";

            List<Trade> trades = new List<Trade>();

            try
            {
                using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                {
                    connection.Open();
                    using (SqlCommand command = new SqlCommand("", connection))
                    {
                        command.CommandType = System.Data.CommandType.Text;
                        command.CommandText = "SELECT t.Id, t.Time, t.Price FROM dbo.Trades t WHERE t.[time] > @date ORDER BY t.Id ASC";
                        command.Parameters.Add(new SqlParameter("date", start));

                        using (SqlDataReader reader = command.ExecuteReader())
                        {


                            while (reader.Read())
                            {
                                var trade = new Trade();

                                trade.Id = reader.GetInt32(0);
                                trade.Date = reader.GetDateTime(1);
                                trade.Price = reader.GetDecimal(2);

                                trades.Add(trade);
                            }
                        }

                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            //Set the trading state
            State = TradingState.Initial;
            Orders = new List<Order>();

            //l h o c

            //Find the first trade
            //Find all trades within the next 5 minutes
            //If there are any trade
            //Calculate low, high, open, close
            if (trades.Any())
            {
                start = trades.First().Date > start ? trades.First().Date : start;

                for (DateTime i = start; i < end; i = i.AddMinutes(interval))
                {
                    var tradesInSameWindow = trades.Where(t => t.Date >= i && t.Date < i.AddMinutes(interval));

                    if (tradesInSameWindow.Any())
                    {
                        var low = tradesInSameWindow.Select(t => t.Price).Min();
                        var high = tradesInSameWindow.Select(t => t.Price).Max();
                        var open = tradesInSameWindow.First().Price;
                        var close = tradesInSameWindow.Last().Price;

                        var ohlc = new OHLC(open, high, low, close);

                        //Check for indicators and make trading decisions
                        MovingAverageStrategy(ohlc, tradesInSameWindow.Last().Date);

                        DataPoints.Insert(0, ohlc);

                        series.Points.AddY(low, high, open, close);
                    }
                }
            }

            //Stats

            ChartPlaceHolder.Controls.Add(chart);
        }

        
        public List<OHLC> DataPoints = new List<OHLC>();

        private decimal CalculateMovingAverage(int dataPointCount)
        {
            return DataPoints.Take(dataPointCount).Average(p => p.Close);
        }

        private double CalculateRelativeStrengthIndex(int dataPointCount)
        {
            decimal sumUp = 0;
            decimal sumDown = 0;

            var dataPoints = DataPoints.Take(dataPointCount).ToList();
            var n = dataPoints.Count;

            //Up sum
            //Down sum
            for (int i = 1; i <n ; i++)
            {
                var change = dataPoints[i].Close - dataPoints[i-1].Close;

                if (change > 0)
                {
                    sumUp += change;
                }
                else
                {
                    sumDown -= change;
                }
            }

            //Means of sums
            var meanUp = sumUp / (n-1);
            var meanDown = sumDown / (n-1);

            //RSI = meanUp/(meanUp+meanDown)
            var rsi = meanUp / (meanUp + meanDown) * 100;

            return (double)rsi;
        }

        public TradingState State { get; set; }

        private void RelativeStrengthIndexStrategy(OHLC ohlc, DateTime date)
        {
            if (DataPoints.Count < 200)
                return;

            //Calculate indicators
            var direction = CalculateMovingAverage(200);
            var sellSignal = CalculateMovingAverage(5);
            var rsi = CalculateRelativeStrengthIndex(3);

            //Long Entry
            //When the price candle closes or is already above 200 day MA and RSI closes below 5 buy
            //Sell when closes above 5-period moving average
            if (State == TradingState.Initial)
            {
                if (ohlc.High > direction && rsi < 5)
                {
                    CreateOrder(ohlc, date, "Buy");
                    State = TradingState.MonitoringDownTrend;
                }
            }
            else if (State == TradingState.MonitoringDownTrend)
            {
                //Stop loss when BUY order loses more than 2% of its value
                var buyOrder = Orders.Last();
                if ((double)((buyOrder.Price - ohlc.Close) / buyOrder.Price) > 0.02)
                {
                    CreateOrder(ohlc, date, "Sell");
                    State = TradingState.Initial;
                    return;//Otherwise might sell twice
                }

                //Limit profit
                //by selling the asset when it closes over its 5-period moving average
                if (ohlc.Close > sellSignal)
                {
                    CreateOrder(ohlc, date, "Sell");
                    State = TradingState.Initial;
                }

            }
        }

        private void MovingAverageStrategy(OHLC ohlc, DateTime date)
        {
            if (DataPoints.Count < 200)
                return;

            //Calculate indicators
            var direction = CalculateMovingAverage(200);
            var correction = CalculateMovingAverage(10);

            //Long Entry
            //When the price candle closes or is already above 200 day MA, then wait for price correction until price drops to 10 day MA, 
            //then when the candle closes above 10 day MA on the upside, the enter the trade. Stop loss would be when price closes below the 10 day MA.
            if (State == TradingState.Initial)
            {
                if (ohlc.High > direction)
                {
                    //wait for price correction
                    State = TradingState.WaitForUpTrendPriceCorrection;
                }
            }

            else if (State == TradingState.WaitForUpTrendPriceCorrection)
            {
                if (ohlc.Low < correction)
                    State = TradingState.WaitingToBuy;
            }
            else if (State == TradingState.WaitingToBuy)
            {
                if (ohlc.Close > correction)
                {
                    CreateOrder(ohlc, date, "Buy");
                    State = TradingState.MonitoringDownTrend;
                }
            }
            else if (State == TradingState.MonitoringDownTrend)
            {
                //Stop loss
                if (ohlc.Close < correction)
                {
                    CreateOrder(ohlc, date, "Sell");
                    State = TradingState.Initial;
                }

                //Limit profit
                //Limit - Profit target would vary with each item. For day traders, 
                //I suggest profit target of 50 % of daily Average Trading Range of that item for the last month.
                var limit = 50; //50 EUR profit limit based on daily average trading range of BTC/EUR first week of last month
                if (ohlc.Close > Orders.Last().Price + limit)
                {
                    CreateOrder(ohlc, date, "Sell");
                    State = TradingState.Initial;
                }
             
            }
        }

        private string CreateOrder(OHLC ohlc, DateTime date, string type)
        {
            var order = new Order()
            {
                Price = ohlc.Close,
                Date = date,
                Type = type
            };

            Orders.Add(order);

            var message = $"{date} {type} 1 BTC at {ohlc.Close}";

            OrdersListBox.Items.Add(new ListItem(message));

            return message;
        }

        public List<Order> Orders { get; set; }

        protected void SimulateButton_Click(object sender, EventArgs e)
        {
            var startDate = DateTime.ParseExact(StartDateTextBox.Text, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(EndDateTextBox.Text, "yyyyMMdd", CultureInfo.InvariantCulture);
            var interval = int.Parse(IntervalDropDownList.SelectedValue);

            Simulate(startDate, endDate, interval);
        }

        protected void OrdersListBox_SelectedIndexChanged(object sender, EventArgs e)
        {

        }
    }

    public class Order
    {
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
    }

    public enum TradingState
    {
        Initial,
        MonitoringUpTrend,
        WaitForUpTrendPriceCorrection,
        WaitingToBuy,
        MonitoringDownTrend,

    }

    public class Stats
    {
        public decimal Pnl { get; set; }
        public double WinRatio { get; set; }
        public int TradePairCount { get; set; }
        public decimal MaxWin { get; set; }
        public decimal MaxLoss { get; set; }
        public decimal MeanWin { get; set; }
        public decimal MeanLoss { get; set; }
        public double MaxHoldInHours { get; set; }
        public double MeanHoldInHours { get; set; }
        public double MinHoldInHours { get; set; }

        public Stats(List<Order> orders)
        {
            if (orders.Any())
            {
                if (orders.First().Type != "Buy")
                    throw new StrategyException($"Strategy first order is {orders.First().Type} but should be Buy");

                //Clean up so we have only Buy-Sell pairs
                if (orders.Last().Type == "Buy")
                    orders.Remove(orders.Last());

                //Sense-check
                if(orders.Count % 2 != 0)
                    throw new StrategyException($"Strategy have odd trades. Failed pairing. {orders.Count}");

                if(orders.Count(o=>o.Type == "Buy") != orders.Count(o => o.Type == "Sell"))
                    throw new StrategyException($"Unbalanced trades. Failed pairing. Buys {orders.Count(o => o.Type == "Buy")} vs Sells {orders.Count(o => o.Type == "Sell")}");

                var tradePairs = new List<TradePair>();

                //Pair buy and sell
                foreach (var order in orders)
                {
                    Debug.WriteLine((order.Type == "Buy" ? "-" : "+") + order.Price);

                    if (order.Type == "Buy")
                    {
                        //TODO: Catch consecutive Buy orders

                        tradePairs.Add(new TradePair() { BuyOrder = order });
                    }
                    else
                    {
                        //Double sell
                        if(tradePairs.Last().SellOrder !=null)
                            throw new StrategyException($"Consecutive sell orders last {tradePairs.Last().SellOrder} and current {order.Price}");

                        tradePairs.Last().SellOrder = order;
                    }
                        
                }

                Pnl = tradePairs.Select(p => p.SellOrder.Price - p.BuyOrder.Price).Sum();
                
                var wins = tradePairs.Where(p => p.SellOrder.Price > p.BuyOrder.Price);
                var losses = tradePairs.Where(p => p.SellOrder.Price <= p.BuyOrder.Price);
                WinRatio = wins.Count()/(double)tradePairs.Count()*100;

                TradePairCount = tradePairs.Count();

                //Checking for anomalies
                MaxWin = wins.Max(p => p.SellOrder.Price - p.BuyOrder.Price);
                MaxLoss = losses.Min(p => p.SellOrder.Price - p.BuyOrder.Price);
                MeanWin = wins.Average(p => p.SellOrder.Price - p.BuyOrder.Price);
                MeanLoss = losses.Average(p => p.SellOrder.Price - p.BuyOrder.Price);

                //Risk metrics - max hold
                MaxHoldInHours = tradePairs.Max(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                MeanHoldInHours = tradePairs.Average(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                MinHoldInHours = tradePairs.Min(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
            }
        }

        private class TradePair
        {
            public Order BuyOrder { get; set; }
            public Order SellOrder { get; set; }
        }

        private class StrategyException : ApplicationException
        {
            public StrategyException(string message) : base(message)
            {

            }
        }
    }
}