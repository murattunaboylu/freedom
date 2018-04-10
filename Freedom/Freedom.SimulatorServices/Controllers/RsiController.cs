using Freedom.Algorithms;
using Freedom.DataAccessLayer;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;
using SendGrid;
using SendGrid.Helpers.Mail;

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
            var connectionName = ConfigurationManager.AppSettings["ConnectionName"];
            var connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;

            List<OHLC> ohlcList = new List<OHLC>();

            try
            {
                using (SqlConnection connection = new SqlConnection(connectionString))
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
                                ohlc.Volume = (volume * 1000);

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
            Account = new Account() { Euro = 10000 };

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

                        var ohlc = new OHLC { Open = open, High = high, Low = low, Close = close, Volume = volume, Start = windowStart, End = windowEnd };

                        DataPoints.Insert(0, ohlc);

                        //Check for indicators and make trading decisions
                        DonchienBreakoutStrategy(ohlc, windowEnd, parameters);

                        //Add Donchian Channels
                        var dca = CalculateDonchianChannel(55);

                        UpperBand.Add((double)dca.UpperBand);
                        LowerBand.Add((double)dca.LowerBand);
                    }
                }
            }

            //Stats after simulation
            var stats = new Stats(Orders, Account, DataPoints)
            {
                Market = DataPoints.First().Close - DataPoints.Last().Close,
                MarketRiskAdjustedReturn = (double)((DataPoints.First().Close - DataPoints.Last().Close) / DataPoints.Last().Close) * 100,
                Target = (end - start).Days * 80, //80 EUR profit per day
            };

            return new SimulationResult()
            {
                Dates = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.End).ToList(),
                Values = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.Close).ToList(),
                Volumes = DataPoints.OrderBy(dp => dp.Start).Select(dp => dp.Volume).ToList(),
                Upper = UpperBand,
                Lower = LowerBand,
                Orders = Orders,
                Events = Events,
                Stats = stats
            };
        }

        public List<double> UpperBand { get; set; } = new List<double>();
        public List<double> LowerBand { get; set; } = new List<double>();

        public List<Event> Events { get; set; } = new List<Event>();

        public decimal CalculateEnvelopeLowerBand(List<OHLC> dataPoints, int period, double weight)
        {
            var closes = dataPoints.Take(period).Select(d => d.Close).ToList();
            var average = closes.Average();
            var standardDeviation = closes.StandardDeviation();
            var lowerBand = average - (decimal)(weight * standardDeviation);

            return lowerBand;
        }

        private double CalculateSlowStochasticOscillatorsPercentK(List<OHLC> dataPoints, int kPeriod, int slowing)
        {
            var percentKs = new List<double>();

            for (int i = 0; i < slowing; i++)
            {
                var dataPointsInPeriod = dataPoints.Skip(i).Take(kPeriod).ToList();
                var lowest = dataPointsInPeriod.Select(d => d.Low).Min();
                var highest = dataPointsInPeriod.Select(d => d.High).Max();
                var latestClose = dataPointsInPeriod.First().Close;
                var percentK = (double)((latestClose - lowest) / (highest - lowest) * 100);

                percentKs.Add(percentK);
            }

            var slowPercentK = percentKs.Average();

            return slowPercentK;

        }

        private double CalculateSlowStochasticOscillatorsPercentD(List<OHLC> dataPoints, int dPeriod, int kPeriod, int slowing)
        {
            var slowPercentKs = new List<double>();

            for (int i = 0; i < dPeriod; i++)
            {
                var dataPointsInPeriod = dataPoints.Skip(i).ToList();
                var slowPercentK = CalculateSlowStochasticOscillatorsPercentK(dataPointsInPeriod, kPeriod, slowing);


                slowPercentKs.Add(slowPercentK);
            }

            var percentD = slowPercentKs.Average();

            return percentD;
        }


        private DonchianChannelAlgorithm.Result CalculateDonchianChannel(int period)
        {
            var dca = new DonchianChannelAlgorithm();
            var result = dca.Calculate(DataPoints, period);

            return result;
        }

        private BollingerBandsAlgorithm.Result CalculateBollingerBands(int period, int sigmaWeight)
        {
            var bb = new BollingerBandsAlgorithm();
            var result = bb.Calculate(DataPoints.Take(period).Select(p => p.Close).ToList(), period, sigmaWeight);

            return result;
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
                Quantity = type == "Buy" ? (double)(Account.Euro / ohlc.Close) : (double)Account.BitCoin,
                Date = date,
                Type = type
            };

            Orders.Add(order);

            if (bool.Parse(ConfigurationManager.AppSettings["EmailSignals"]))
            {
                SendEmail($"{order.Type} Signal", $"{order.Type} {order.Quantity} @ {order.Price} - {order.Date}");
            }

            //Settle the account as if the order is immediately executed
            if (type == "Buy")
            {
                Account.BitCoin = (double)(Account.Euro / ohlc.Close);
                Account.Euro = 0;
            }
            else
            {
                Account.Euro = (decimal)Account.BitCoin * ohlc.Close;
                Account.BitCoin = 0;
            }

            var message = $"{date} {type} {order.Quantity} BTC at {order.Price}";

            Events.Add(new Event(date, type, "@" + ohlc.Close + " " + description));

            return message;
        }

        private void SendEmail(string title, string message)
        {
            var client = new SendGridClient(ConfigurationManager.AppSettings["SendGridApiKey"]);
            var from = new EmailAddress("murat.tunaboylu@svarlight.com", "Freedom");
            var to = new EmailAddress("murattunaboylu@gmail.com", "Murat Tunaboylu");
            var body = message;
            var msg = MailHelper.CreateSingleEmail(from, to, title, body, "");

            var x = client.SendEmailAsync(msg);
            var r = x.Result;
        }

        public List<Order> Orders { get; set; }

        public List<OHLC> DataPoints = new List<OHLC>();

        public Account Account { get; set; }

        private decimal CalculateMovingAverage(int dataPointCount)
        {
            return DataPoints.Take(dataPointCount).Average(p => p.Close);
        }

        private double CalculateRelativeVigorIndex(int dataPointCount)
        {
            //rvi = (C-O)/(H-L)

            var dataPoints = DataPoints.Take(dataPointCount).ToList();
            var rvis = dataPoints.Select(d => (d.Close - d.Open) / (d.High - d.Low + 0.01m));
            var rvi = (double)(rvis.Average() * 100);
            return rvi;
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
            var bb = CalculateBollingerBands(20, 2);
            var percentB = bb.PercentB;
            var bandwidth = bb.Bandwidth;

            //Long Entry
            //When the price candle closes or is already above 200 day MA and RSI closes below 5 buy
            //Sell when closes above 5-period moving average
            if (State == TradingState.Initial)
            {
                if (ohlc.High > direction && rsi < rsiThreshold)
                {
                    CreateOrder(ohlc, date, "Buy", $"<br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.MonitoringDownTrend;
                }
            }
            else if (State == TradingState.MonitoringDownTrend)
            {
                //Stop loss when BUY order loses more than 2% of its value
                var buyOrder = Orders.Last();
                if ((double)((buyOrder.Price - ohlc.Close) / buyOrder.Price) > stopLossRatio)
                {
                    CreateOrder(ohlc, date, "Sell", $"Stop Loss <br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.Initial;
                    return;//Otherwise might sell twice
                }

                //Limit profit
                //by selling the asset when it closes over its 5-period moving average
                if (ohlc.Close > sellSignal && ohlc.Close > buyOrder.Price)
                {
                    CreateOrder(ohlc, date, "Sell", $"Closes over 5-d MA <br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.Initial;
                }

            }
        }

        private void BollingerBandStrategy(OHLC ohlc, DateTime date, StrategyParameters parameters)
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
            var bb = CalculateBollingerBands(20, 2);
            var percentB = bb.PercentB;
            var bandwidth = bb.Bandwidth;

            //Long Entry
            //When the price candle closes or is already above 200 day MA and RSI closes below 5 buy
            //Sell when closes above 5-period moving average
            if (State == TradingState.Initial)
            {
                if (ohlc.High > direction && percentB > 100)
                {
                    CreateOrder(ohlc, date, "Buy", $"<br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.MonitoringDownTrend;
                }
            }
            else if (State == TradingState.MonitoringDownTrend || State == TradingState.WaitingToSell)
            {
                //Stop loss when BUY order loses more than 2% of its value
                var buyOrder = Orders.Last();
                if ((double)((buyOrder.Price - ohlc.Close) / buyOrder.Price) > stopLossRatio)
                {
                    CreateOrder(ohlc, date, "Sell", $"Stop Loss <br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.Initial;
                    return;//Otherwise might sell twice
                }


                //Limit profit on downward swing
                if (percentB < 0)
                {
                    CreateOrder(ohlc, date, "Sell", $"%b hit over 100 <br/> %b {percentB:N0} <br/> bandwidth {bandwidth:N0} <br/> upper {bb.UpperBand:N0} <br/> lower {bb.LowerBand:N0}");
                    State = TradingState.Initial;
                    return;
                }
            }
        }

        private void DonchienBreakoutStrategy(OHLC ohlc, DateTime date, StrategyParameters parameters)
        {
            if (DataPoints.Count < 55)
                return;

            //Calculate indicators
            var dca = CalculateDonchianChannel(55);

            //Long Entry
            //When closes at DC upper limit
            if (State == TradingState.Initial)
            {
                if (ohlc.Close >= dca.UpperBand)
                {
                    CreateOrder(ohlc, date, "Buy", $"<br/> Close {ohlc.Close:N0} > Upper {dca.UpperBand:N0}");
                    State = TradingState.WaitingToSell;
                }
            }
            else if (State == TradingState.WaitingToSell)
            {
                //Stop loss when BUY order loses 70 EUR
                var buyOrder = Orders.Last();
                var loss = buyOrder.Price - ohlc.Close;
                if (loss > 70)
                {
                    CreateOrder(ohlc, date, "Sell", $"Stop Loss <br/> at loss {loss:N0}");
                    State = TradingState.Initial;
                    return;//Otherwise might sell twice
                }

                //Exit when closes at mid point
                if (ohlc.Close <= (dca.UpperBand - dca.LowerBand) / 2 + dca.LowerBand)
                {
                    CreateOrder(ohlc, date, "Sell", $"Closes at mid point");
                    State = TradingState.Initial;
                    return;
                }
            }
        }

        private void RelativeVigorIndexStrategy(OHLC ohlc, DateTime date, StrategyParameters parameters)
        {
            if (DataPoints.Count < 40)
                return;

            //Calculate indicators
            var rvi = CalculateRelativeVigorIndex(22);
            var rviBase = CalculateRelativeVigorIndex(10);
            var percentK = CalculateSlowStochasticOscillatorsPercentK(DataPoints, 17, 6);
            var percentD = CalculateSlowStochasticOscillatorsPercentD(DataPoints, 13, 17, 6);
            var envelopeLowerBand = CalculateEnvelopeLowerBand(DataPoints, 10, 0.97);

            //Long Entry
            //When the RVI is greater than the signal and 
            //percent K is higher than percent D
            if (State == TradingState.Initial)
            {
                if (rvi > rviBase && percentK > percentD)
                {
                    CreateOrder(ohlc, date, "Buy", $"<br/> %b {rvi:N0} <br/> > {rviBase:N0} <br/> and {percentK:N0} <br/> > {percentD:N0}");
                    State = TradingState.WaitingToSell;
                }
            }
            else if (State == TradingState.WaitingToSell || State == TradingState.MonitoringDownTrend)
            {
                //Stop loss when BUY order loses 70 EUR
                var buyOrder = Orders.Last();
                //var loss = buyOrder.Price - ohlc.Close;
                //if (loss > 70)
                //{
                //    CreateOrder(ohlc, date, "Sell", $"Stop Loss <br/> at loss {loss:N0}");
                //    State = TradingState.Initial;
                //    return;//Otherwise might sell twice
                //}

                //Exit when it closes over envelope lower band 
                //after closing under envelope lower band
                if (State == TradingState.WaitingToSell)
                {
                    if (ohlc.Close < envelopeLowerBand)
                        State = TradingState.MonitoringDownTrend;
                }
                else if (State == TradingState.MonitoringDownTrend)
                {
                    if (ohlc.Close > envelopeLowerBand)
                    {
                        CreateOrder(ohlc, date, "Sell", $"Closes over envelope lower band after closing under");
                        State = TradingState.Initial;
                        return;
                    }
                }

                //Limit profit
                //by selling the asset when it brings in 200 EUR
                var profit = ohlc.Close - buyOrder.Price;
                var profitLimit = 200;
                if (ohlc.Close - buyOrder.Price >= profitLimit)
                {
                    CreateOrder(ohlc, date, "Sell", $"Profit {profit:N0} exceeded {profitLimit} EUR limit");
                    State = TradingState.Initial;
                    return;
                }

            }
        }

    }
}
