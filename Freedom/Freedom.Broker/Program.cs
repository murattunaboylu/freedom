using System;
using System.Configuration;
using System.Globalization;
using System.Linq;

namespace Freedom.Broker
{
    class Program
    {
        static void Main(string[] args)
        {         
            //Run the algo
            var simulationManager = new SimulationManager();

            var start = ConfigurationManager.AppSettings["StartDate"] ?? "20170701";
            var end = DateTime.Now.AddDays(1).ToString("yyyyMMdd");
            var results = simulationManager.GetProductAsync($"rsi/{start}/{end}/120/").Result;

            //Report daily overall status
            var reportManager = new ReportManager();

            if (DateTime.Now.Hour == 8 || DateTime.Now.Hour == 9)
                reportManager.SendEmail("Daily", results?.Stats.NetProfit.ToString(CultureInfo.InvariantCulture));

            //Look for signals
            if (results?.Orders != null)
            {
                var signals = results.Orders.Where(o => o.Date > DateTime.Now.AddHours(-3)).ToList();

                if (signals.Any())
                {
                    var signal = signals.Last();

                    //Report new signal
                    reportManager.SendEmail( $"{signal.Type} Signal", $"{signal.Type} {signal.Quantity} @ {signal.Price} - {signal.Date}");
                }
            }
        }
    }
}
