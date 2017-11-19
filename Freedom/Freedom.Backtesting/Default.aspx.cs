using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.DataVisualization.Charting;
using System.Web.UI.WebControls;

namespace Freedom.Backtesting
{
    public partial class _Default : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
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
                        command.Parameters.Add(new SqlParameter("date", DateTime.Now.AddDays(-5)));

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
                var startDate = trades.First().Date;

                var intervalInMinutes = 5;

                for (DateTime i = startDate; i < startDate.AddDays(4); i = i.AddMinutes(intervalInMinutes))
                {
                    var tradesInSameWindow = trades.Where(t => t.Date >= i && t.Date < i.AddMinutes(intervalInMinutes));

                    if (tradesInSameWindow.Any())
                    {
                        var low = tradesInSameWindow.Select(t => t.Price).Min();
                        var high = tradesInSameWindow.Select(t => t.Price).Max();
                        var open = tradesInSameWindow.First().Price;
                        var close = tradesInSameWindow.Last().Price;

                        var ohlc = new OHLC(open, high, low, close);

                        //Check for indicators and make trading decisions
                        Trade(ohlc, tradesInSameWindow.Last().Date);

                        DataPoints.Insert(0, ohlc);

                        series.Points.AddY(low, high, open, close);
                    }
                }
            }

            PnLLabel.Text = CalculatePnL().ToString();


            ChartPlaceHolder.Controls.Add(chart);
        }

        private decimal CalculatePnL()
        {
            decimal pnl = 0;

            if (Orders.Any())
            {
                if (Orders.Last().Type == "Buy")
                    Orders.Remove(Orders.Last());

                foreach (var order in Orders)
                {
                    pnl = order.Type == "Buy" ? pnl - order.Price : pnl + order.Price;
                }
            }

            return pnl;
        }

        public List<OHLC> DataPoints = new List<OHLC>();

        private decimal CalculateMovingAverage(int dataPointCount)
        {
            return DataPoints.Take(dataPointCount).Average(p => p.Close);
        }


        public TradingState State { get; set; }

        private void Trade(OHLC ohlc, DateTime date)
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
}