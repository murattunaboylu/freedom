using Freedom.Algorithms;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Remoting.Messaging;
using System.Web.Http;
using System.Web.Http.Cors;

namespace Freedom.SimulatorServices.Controllers
{

    public class ExportController : ApiController
    {
        [EnableCors("*", "*", "*")]
        public IHttpActionResult Get(string start, string end, int interval, [FromUri] StrategyParameters parameters)
        {
            var startDate = DateTime.ParseExact(start, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(end, "yyyyMMdd", CultureInfo.InvariantCulture);
            var intervalInMinutes = interval;

            var exportLines = Export(startDate, endDate, intervalInMinutes, parameters);
            var export = exportLines.Aggregate((sum, s) => sum + Environment.NewLine + s);

            var result = new HttpResponseMessage(HttpStatusCode.OK);

            result.Content = new StringContent(export);

            result.Content.Headers.ContentDisposition = new ContentDispositionHeaderValue("attachment")
            {
                FileName = $"marketdata.export.{DateTime.Now:yyyMMddHHmmss}.csv"
            };

            result.Content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

            var response = ResponseMessage(result);

            return response;
        }

        public List<string> Export(DateTime start, DateTime end, int interval, StrategyParameters parameters)
        {
            //Read the OHLC from database

            var connectionString = ConfigurationManager.ConnectionStrings["marketdata-local"].ConnectionString;

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
                        var low = Math.Round(ohlcInTheSameWindow.Select(t => t.Low).Min(),2);
                        var high = Math.Round(ohlcInTheSameWindow.Select(t => t.High).Max(), 2);
                        var open = Math.Round(ohlcInTheSameWindow.First().Open, 2);
                        var close = Math.Round(ohlcInTheSameWindow.Last().Close, 2);
                        var volume = Math.Round(ohlcInTheSameWindow.Sum(t => t.Volume), 2);

                        var ohlc = new OhlcIndicators(open, high, low, close)
                        {
                            Volume = volume, Start = windowStart, End = windowEnd,
                        };

                        DataPoints.Add(ohlc);

                        var prices = DataPoints.Select(d => d.Close).ToList();

                        ohlc.Mva10 = Math.Round(CalculateMovingAverage(prices, 10), 2);
                        ohlc.Mva200 = Math.Round(CalculateMovingAverage(prices, 200), 2);
                        ohlc.Rsi2 = Math.Round(CalculateRelativeStrengthIndex(prices, 3), 2);
                        ohlc.Rsi14 = Math.Round(CalculateRelativeStrengthIndex(prices, 15), 2);
                        var bb = CalculateBollingerBands(prices, 20, 2);
                        ohlc.PercentB = Math.Round(bb.PercentB, 2);
                        ohlc.Bandwidth = Math.Round(bb.Bandwidth, 2);
                    }
                }
            }

            var exportLines = DataPoints.Select(d => $"{d.Start:M/d/yyyy H:mm},{d.Open},{d.High},{d.Low},{d.Close},{d.Volume},{d.Mva10},{d.Mva200},{d.Rsi2},{d.Rsi14},{d.PercentB},{d.Bandwidth}").ToList();

            exportLines.Insert(0, "Date, Open, High, Low, Close, Volume, Mva10, Mva200, Rsi2, Rsi14, PercentB, Bandwidth");

            return exportLines;
        }

        private decimal CalculateMovingAverage(List<decimal> prices, int dataPointCount)
        {
            return prices.Skip(prices.Count - dataPointCount).Take(dataPointCount).Average();
        }

        private double CalculateRelativeStrengthIndex(List<decimal> prices, int dataPointCount)
        {
            decimal sumUp = 0;
            decimal sumDown = 0;

            var dataPoints = prices.Skip(prices.Count - dataPointCount).Take(dataPointCount).ToList();
            var n = dataPoints.Count;

            if (n == 1)
                return 50;

            //Up sum
            //Down sum
            for (int i = 1; i < n; i++)
            {
                var change = dataPoints[i] - dataPoints[i - 1];

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

        private BollingerBandsAlgorithm.Result CalculateBollingerBands(List<decimal> prices, int period, int sigmaWeight)
        {
            var bb = new BollingerBandsAlgorithm();
            var result = bb.Calculate(prices.Skip(prices.Count - period).Take(period).ToList(), period, sigmaWeight);

            return result;
        }

        public List<OhlcIndicators> DataPoints { get; set; } = new List<OhlcIndicators>();
    }
}
