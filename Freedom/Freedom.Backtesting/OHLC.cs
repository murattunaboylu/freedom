using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Freedom.Backtesting
{
    public class OHLC
    {
        public OHLC(decimal open, decimal high, decimal low, decimal close)
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public OHLC()
        {

        }

        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }
}