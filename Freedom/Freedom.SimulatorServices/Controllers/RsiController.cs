using System;
using System.Collections.Generic;
using System.Globalization;
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

            var result = new SimulationResult()
            {
                Description = $"Running simulation {startDate}-{endDate} for every {intervalInMinutes} minutes with RSI period {parameters.RsiPeriod}",
                Values = new List<decimal>() { 210.35m, 223, 230.35m, 243, 219.35m, 203, 212.35m, 224, 219.35m, 225 },
                Volumes = new List<int>() { 23500, 9500, 13560, 19230, 23500, 9500, 13560, 19230, 1600, 5962 }
            };

            return result;
        }
    }

    public class StrategyParameters
    {
        public int RsiPeriod { get; set; }
    }

    public class SimulationResult
    {
        public string Description { get; set; }
        public List<decimal> Values { get; set; }
        public List<int> Volumes { get; set; }
    }
}
