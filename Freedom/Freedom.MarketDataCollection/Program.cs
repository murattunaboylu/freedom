using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Freedom.MarketDataCollection
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Enter to start reading market data from CEX.IO");
            Console.ReadLine();
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
            if (Latest == null)
            {
                HttpResponseMessage response = await client.GetAsync(path);
                if (response.IsSuccessStatusCode)
                {
                    var tradesJson = await response.Content.ReadAsStringAsync();
                    trades = JsonConvert.DeserializeObject<List<Trade>>(tradesJson);

                    Latest = trades.First();
                }
            }
            else
            {
                HttpResponseMessage response = await client.GetAsync(path + "?since=" + Latest.Id);
                if (response.IsSuccessStatusCode)
                {
                    var tradesJson = await response.Content.ReadAsStringAsync();
                    trades = JsonConvert.DeserializeObject<List<Trade>>(tradesJson, new JavaScriptDateTimeConverter());

                    Latest = trades.First();
                }
            }
        
            return trades;
        }

        public static Trade Latest = null;
        public static List<Trade> trades = null;


        static async Task RunAsync()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

            client.BaseAddress = new Uri("https://cex.io/api/");
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                while (true)
                {
                    var trades = await GetTradeAsync("trade_history/BTC/EUR/");
                    Console.WriteLine(trades.Count);
                    Console.WriteLine(Latest.Id);
                    Console.WriteLine(Latest.Amount);
                    Console.WriteLine(Latest.Price);
                    Console.WriteLine(Latest.Date);
                    System.Threading.Thread.Sleep(1000);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.ReadLine();
        }
    }
}
