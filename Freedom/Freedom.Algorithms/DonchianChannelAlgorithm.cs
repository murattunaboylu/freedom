using Freedom.DataAccessLayer;
using System.Collections.Generic;
using System.Linq;

namespace Freedom.Algorithms
{
    public class DonchianChannelAlgorithm
    {
        public Result Calculate(List<OHLC> values, int period)
        {
            var result = new Result();

            var selected = values.Take(period);

            result.UpperBand = selected.Max(x => x.High);
            result.LowerBand = selected.Min(x => x.Low);

            return result;
        }

        public class Result
        {
            public decimal UpperBand { get; set; }
            public decimal LowerBand { get; set; }
        }
    }


}
