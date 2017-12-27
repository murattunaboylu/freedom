using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Freedom.SimulatorServices
{
    public class OhlcIndicators : OHLC
    {
        public OhlcIndicators(decimal open, decimal high, decimal low, decimal close) : base(open, high, low, close)
        {
        }

        public decimal Mva10 { get; set; }
        public decimal Mva200 { get; set; }
        public double Rsi2 { get; set; }
        public double Rsi14 { get; set; }
        public double PercentB { get; set; }
        public double Bandwidth { get; set; }
    }
}