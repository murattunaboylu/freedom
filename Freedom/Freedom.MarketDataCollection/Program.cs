using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.IO;
using System.Data.SqlClient;

namespace Freedom.MarketDataCollection
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter to start reading market data from CEX.IO");
            //Console.ReadLine();
            RunAsync().Wait();

            //https://cex.io/api/ohlcv/hd/20160228/BTC/USD

            //Start the by calling https://cex.io/api/trade_history/BTC/EUR/
            //which will return the latest 1000 trades
            //then loop with https://cex.io/api/trade_history/BTC/EUR/?since=tid
            //passing the latest tid to get all trades 
            //
            //API is limited to 600 requests per 10 minutes. 1 request/second
            //Not a problem if trades per second is less than 1000
            //
            //Implementation of web call https://docs.microsoft.com/en-us/aspnet/web-api/overview/advanced/calling-a-web-api-from-a-net-client


            //https://cex.io/api/trade_history/BTC/USD/?since=1 
        }

        static HttpClient client = new HttpClient();

        static async Task<List<Trade>> GetTradeAsync(string path)
        {
            List<Trade> trades = new List<Trade>();

            if (!Trades.Any())
            {
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var tradesJson = await response.Content.ReadAsStringAsync();
                    trades = JsonConvert.DeserializeObject<List<Trade>>(tradesJson);

                    trades.Reverse();
                }
            }
            else
            {
                HttpResponseMessage response = await client.GetAsync(path + "?since=" + Trades.Last().Id + 1);
                if (response.IsSuccessStatusCode)
                {
                    var tradesJson = await response.Content.ReadAsStringAsync();
                    trades = JsonConvert.DeserializeObject<List<Trade>>(tradesJson, new JavaScriptDateTimeConverter());

                    if (trades.Any())
                    {
                        trades.Reverse();
                    }
                }
            }

            return trades;
        }

        private static readonly List<Trade> Trades = new List<Trade>();


        static async Task RunAsync()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            client.BaseAddress = new Uri("https://cex.io/api/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            if (!Directory.Exists("Data"))
                Directory.CreateDirectory("Data");

            //Write the trades into a database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "ff-marketdata.database.windows.net";
            builder.UserID = "marketdata";
            builder.Password = "mar20X/b";
            builder.InitialCatalog = "marketdata";


            using (FileStream tradesFile = File.OpenWrite("Data\\trades.csv"))
            {
                using (StreamWriter writer = new StreamWriter(tradesFile))
                {
                    try
                    {
                        using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
                        {
                            connection.Open();
                            using (SqlCommand command = new SqlCommand("", connection))
                            {
                                command.CommandType = System.Data.CommandType.Text;
                                command.CommandText = "INSERT INTO dbo.Trades (Exchange, Market, TradeId, Price, Quantity, Total, [Time], [Type]) " +
                                    "VALUES(@Exchange, @Market, @TradeId, @Price, @Quantity, @Total, @Time, @Type)";

                                command.Parameters.Add(new SqlParameter("Exchange", ""));
                                command.Parameters.Add(new SqlParameter("Market", ""));
                                command.Parameters.Add(new SqlParameter("TradeId", ""));
                                command.Parameters.Add(new SqlParameter("Price", System.Data.SqlDbType.Money));
                                command.Parameters.Add(new SqlParameter("Quantity", System.Data.SqlDbType.Decimal));
                                command.Parameters.Add(new SqlParameter("Total", System.Data.SqlDbType.Decimal));
                                command.Parameters.Add(new SqlParameter("Time", System.Data.SqlDbType.DateTime));
                                command.Parameters.Add(new SqlParameter("Type", ""));

                                command.Parameters["Quantity"].Precision = 18;
                                command.Parameters["Quantity"].Scale = 8;

                                command.Parameters["Total"].Precision = 18;
                                command.Parameters["Total"].Scale = 8;

                                while (true)
                                {
                                    var trades = await GetTradeAsync("trade_history/BTC/EUR/");

                                    if (trades.Any())
                                    {
                                        Console.WriteLine(trades.Count + " new trades");

                                        Trades.AddRange(trades);

                                        //Save the trades to a file
                                        foreach (var trade in trades)
                                        {
                                            Console.WriteLine(trade);
                                            var tradeJson = JsonConvert.SerializeObject(trade);
                                            writer.WriteLine(tradeJson);

                                            command.Parameters["Exchange"].Value = "CEX.IO";
                                            command.Parameters["Market"].Value = "BTC/EUR";
                                            command.Parameters["TradeId"].Value = trade.Id;
                                            command.Parameters["Price"].Value = trade.Price;
                                            command.Parameters["Quantity"].Value = trade.Amount;
                                            command.Parameters["Total"].Value = trade.Price * trade.Amount;
                                            command.Parameters["Time"].Value = trade.Date;
                                            command.Parameters["Type"].Value = trade.Type;

                                            command.ExecuteNonQuery();
                                        }
                                    }

                                    System.Threading.Thread.Sleep(120*1000);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }

            Console.ReadLine();
        }
    }
}
