using System;
using System.Collections.Generic;
using System.Globalization;
using System.Web.Http;

namespace Freedom.SimulatorServices.Controllers
{
    public class RsiController : ApiController
    {
        public SimulationResult Get(string start, string end, int interval, [FromUri] StrategyParameters parameters)
        {
            var startDate = DateTime.ParseExact(start, "yyyyMMdd", CultureInfo.InvariantCulture);
            var endDate = DateTime.ParseExact(end, "yyyyMMdd", CultureInfo.InvariantCulture);
            var intervalInMinutes = interval;

            var result = new SimulationResult()
            {
                Description = $"Running simulation {startDate}-{endDate} for every {intervalInMinutes} minutes with RSI period {parameters.RsiPeriod}",
                Values = new List<decimal>() { 210.35m, 223,9},
                Volumes = new List<int>() { 23500, 9500}
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
