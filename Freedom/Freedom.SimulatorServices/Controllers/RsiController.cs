using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Freedom.SimulatorServices.Controllers
{

    public class RsiController : ApiController
    {
        [EnableCors("*", "*", "*")]
        public SimulationResult Get(string start, string end, int interval, [FromUri] StrategyParameters parameters)
        {
            var startDate = DateTime.ParseExact(start, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(end, "yyyyMMdd", CultureInfo.InvariantCulture);
            var intervalInMinutes = interval;

            var result = Simulate(startDate, endDate, intervalInMinutes, parameters);

            return result;
        }

        public SimulationResult Simulate(DateTime start, DateTime end, int interval, StrategyParameters parameters)
        {
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
                        command.CommandText = "SELECT o.Id, o.[Open], o.High, o.Low, o.[Close], Start, Volume FROM dbo.OHLC o " +
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
                                var volume = reader.GetDecimal(6);
                                ohlc.Volume = (double)(volume * 1000);

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
                    var windowStart = i;
                    var windowEnd = windowStart.AddMinutes(interval);

                    var ohlcInTheSameWindow = ohlcList.Where(o => o.Start >= windowStart && o.Start < windowEnd).ToList();

                    if (ohlcInTheSameWindow.Any())
                    {
                        var low = ohlcInTheSameWindow.Select(t => t.Low).Min();
                        var high = ohlcInTheSameWindow.Select(t => t.High).Max();
                        var open = ohlcInTheSameWindow.First().Open;
                        var close = ohlcInTheSameWindow.Last().Close;
                        var volume = ohlcInTheSameWindow.Sum(t => t.Volume);
                     
                        var ohlc = new OHLC(open, high, low, close) { Volume = volume, Start = windowStart, End = windowEnd };

                        //Check for indicators and make trading decisions
                        RelativeStrengthIndexStrategy(ohlc, windowEnd, parameters);

                        DataPoints.Insert(0, ohlc);
                    }
                }
            }

            //Stats after simulation
            var stats = new Stats(Orders)
            {
                Market = DataPoints.First().Close - DataPoints.Last().Close,
                Target = (end - start).Days * 80 //80 EUR profit per day 
            };

            return new SimulationResult()
            {
                Dates = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.End).ToList(),
                Values = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.Close).ToList(),
                Volumes = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.Volume).ToList(),
                Orders = Orders,
                Events = Events,
                Stats = stats
            };
        }

        public List<Event> Events { get; set; } = new List<Event>();

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
                    CreateOrder(ohlc, date, "Sell", "Stop loss");
                    State = TradingState.Initial;
                }

                //Limit profit
                //Limit - Profit target would vary with each item. For day traders, 
                //I suggest profit target of 50 % of daily Average Trading Range of that item for the last month.
                var limit = 50; //50 EUR profit limit based on daily average trading range of BTC/EUR first week of last month
                if (ohlc.Close > Orders.Last().Price + limit)
                {
                    CreateOrder(ohlc, date, "Sell", "Profit limit");
                    State = TradingState.Initial;
                }

            }
        }

        private string CreateOrder(OHLC ohlc, DateTime date, string type, string description = "")
        {
            var order = new Order()
            {
                Price = ohlc.Close,
                Date = date,
                Type = type
            };

            Orders.Add(order);

            var message = $"{date} {type} 1 BTC at {ohlc.Close}";

            Events.Add(new Event(date, type, "@" + ohlc.Close + " " + description));

            return message;
        }

        public List<Order> Orders { get; set; }

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
            for (int i = 1; i < n; i++)
            {
                var change = dataPoints[i].Close - dataPoints[i - 1].Close;

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
            var meanUp = sumUp / (n - 1);
            var meanDown = sumDown / (n - 1);

            //RSI = meanUp/(meanUp+meanDown)
            //Preventing divide by 0 - by checking meanUp and Down equality
            var rsi = meanUp == meanDown ? 0.5m : meanUp / (meanUp + meanDown) * 100;

            return (double)rsi;
        }

        public TradingState State { get; set; }

        private void RelativeStrengthIndexStrategy(OHLC ohlc, DateTime date, StrategyParameters parameters)
        {
            if (DataPoints.Count < 200)
                return;

            var rsiPeriod = parameters.RsiPeriod;
            var rsiThreshold = parameters.RsiThreshold;
            var stopLossRatio = parameters.StopLossRatio;

            //Calculate indicators
            var direction = CalculateMovingAverage(200);
            var sellSignal = CalculateMovingAverage(5);
            var rsi = CalculateRelativeStrengthIndex(rsiPeriod);

            //Long Entry
            //When the price candle closes or is already above 200 day MA and RSI closes below 5 buy
            //Sell when closes above 5-period moving average
            if (State == TradingState.Initial)
            {
                if (ohlc.High > direction && rsi < rsiThreshold)
                {
                    CreateOrder(ohlc, date, "Buy");
                    State = TradingState.MonitoringDownTrend;
                }
            }
            else if (State == TradingState.MonitoringDownTrend)
            {
                //Stop loss when BUY order loses more than 2% of its value
                var buyOrder = Orders.Last();
                if ((double)((buyOrder.Price - ohlc.Close) / buyOrder.Price) > stopLossRatio)
                {
                    CreateOrder(ohlc, date, "Sell", "Stop Loss");
                    State = TradingState.Initial;
                    return;//Otherwise might sell twice
                }

                //Limit profit
                //by selling the asset when it closes over its 5-period moving average
                if (ohlc.Close > sellSignal && ohlc.Close > buyOrder.Price)
                {
                    CreateOrder(ohlc, date, "Sell", "Closes over 5-d MA");
                    State = TradingState.Initial;
                }

            }
        }
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
        public decimal Market { get; set; }
        public decimal Target { get; set; }
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
                if (orders.Count % 2 != 0)
                    throw new StrategyException($"Strategy have odd trades. Failed pairing. {orders.Count}");

                if (orders.Count(o => o.Type == "Buy") != orders.Count(o => o.Type == "Sell"))
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
                        if (tradePairs.Last().SellOrder != null)
                            throw new StrategyException($"Consecutive sell orders last {tradePairs.Last().SellOrder} and current {order.Price}");

                        tradePairs.Last().SellOrder = order;
                    }

                }

                Pnl = tradePairs.Select(p => p.SellOrder.Price - p.BuyOrder.Price).Sum();

                var wins = tradePairs.Where(p => p.SellOrder.Price > p.BuyOrder.Price);
                var losses = tradePairs.Where(p => p.SellOrder.Price <= p.BuyOrder.Price);
                WinRatio = wins.Count() / (double)tradePairs.Count() * 100;

                TradePairCount = tradePairs.Count();

                //Checking for anomalies
                if (wins.Any())
                {
                    MaxWin = wins.Max(p => p.SellOrder.Price - p.BuyOrder.Price);
                    MeanWin = wins.Average(p => p.SellOrder.Price - p.BuyOrder.Price);
                }

                if (losses.Any())
                {
                    MaxLoss = losses.Min(p => p.SellOrder.Price - p.BuyOrder.Price);
                    MeanLoss = losses.Average(p => p.SellOrder.Price - p.BuyOrder.Price);
                }

                if (TradePairCount > 0)
                {
                    //Risk metrics - max hold
                    MaxHoldInHours = tradePairs.Max(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                    MeanHoldInHours = tradePairs.Average(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                    MinHoldInHours = tradePairs.Min(p => (p.SellOrder.Date - p.BuyOrder.Date).TotalHours);
                }
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

    public class StrategyParameters
    {
        public int RsiPeriod { get; set; }
        public int RsiThreshold { get; set; }
        public double StopLossRatio { get; set; }
    }

    public class SimulationResult
    {
        public string Description { get; set; }
        public List<decimal> Values { get; set; }
        public List<double> Volumes { get; set; }
        public Stats Stats { get; set; }
        public List<Order> Orders { get; set; }
        public List<DateTime> Dates { get; internal set; }
        public List<Event> Events { get; internal set; }
    }

    public class Event
    {
        public Event(DateTime _date, string _text, string _description)
        {
            //Adding one minute to make the hour round
            //Otherwise events do not show up on close inspection 
            date = _date.AddMinutes(1).ToString("ddd MMM dd yyyy HH:mm:ss");
            type = "sign";
            graph = "graph1";
            backgroundColor = "#85CDE6";
            text = _text.First().ToString();
            description = _text + " " + _description ;
        }

        public string date { get; set; }
        public string type { get; set; }
        public string backgroundColor { get; set; }
        public string graph { get; set; }
        public string text { get; set; }
        public string description { get; set; }
    }

    public class Order
    {
        public decimal Price { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
    }
}
