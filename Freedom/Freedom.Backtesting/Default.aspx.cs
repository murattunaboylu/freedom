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
              

        /// <summary>
        /// Leaving here for future development
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="interval"></param>
        //public void RealTime(DateTime start, DateTime end, int interval)
        //{
        //    //Clear           
        //    OrdersListBox.Items.Clear();
        //    PnLLabel.Text = "0";

        //    //https://msdn.microsoft.com/en-us/library/hh297114%28v=vs.100%29.aspx?f=255&MSPPError=-2147217396
        //    var chart = new Chart();
        //    chart.ImageLocation = "~/TempImages/ChartPic_#SEQ(300,3)";
        //    chart.ChartAreas.Add(new ChartArea("MainArea"));
        //    chart.Width = 800;
        //    var series = new Series() { ChartType = SeriesChartType.Candlestick };
        //    chart.Series.Add(series);

        //    //Read the trades from database
        //    SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
        //    builder.DataSource = "ff-marketdata.database.windows.net";
        //    builder.UserID = "marketdata";
        //    builder.Password = "mar20X/b";
        //    builder.InitialCatalog = "marketdata";

        //    List<Trade> trades = new List<Trade>();

        //    try
        //    {
        //        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
        //        {
        //            connection.Open();
        //            using (SqlCommand command = new SqlCommand("", connection))
        //            {
        //                command.CommandType = System.Data.CommandType.Text;
        //                command.CommandText = "SELECT t.Id, t.Time, t.Price FROM dbo.Trades t WHERE t.[time] > @date ORDER BY t.Id ASC";
        //                command.Parameters.Add(new SqlParameter("date", start));

        //                using (SqlDataReader reader = command.ExecuteReader())
        //                {


        //                    while (reader.Read())
        //                    {
        //                        var trade = new Trade();

        //                        trade.Id = reader.GetInt32(0);
        //                        trade.Date = reader.GetDateTime(1);
        //                        trade.Price = reader.GetDecimal(2);

        //                        trades.Add(trade);
        //                    }
        //                }

        //            }
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        throw ex;
        //    }

        //    //Set the trading state
        //    State = TradingState.Initial;
        //    Orders = new List<Order>();

        //    //l h o c

        //    //Find the first trade
        //    //Find all trades within the next 5 minutes
        //    //If there are any trade
        //    //Calculate low, high, open, close
        //    if (trades.Any())
        //    {
        //        start = trades.First().Date > start ? trades.First().Date : start;

        //        for (DateTime i = start; i < end; i = i.AddMinutes(interval))
        //        {
        //            var tradesInSameWindow = trades.Where(t => t.Date >= i && t.Date < i.AddMinutes(interval)).ToList();

        //            if (tradesInSameWindow.Any())
        //            {
        //                var low = tradesInSameWindow.Select(t => t.Price).Min();
        //                var high = tradesInSameWindow.Select(t => t.Price).Max();
        //                var open = tradesInSameWindow.First().Price;
        //                var close = tradesInSameWindow.Last().Price;

        //                var ohlc = new OHLC(open, high, low, close);

        //                //Check for indicators and make trading decisions
        //                MovingAverageStrategy(ohlc, tradesInSameWindow.Last().Date);

        //                DataPoints.Insert(0, ohlc);

        //                series.Points.AddY(low, high, open, close);
        //            }
        //        }
        //    }

        //    //Stats

        //    ChartPlaceHolder.Controls.Add(chart);
        //}
    
    }



   
   
}