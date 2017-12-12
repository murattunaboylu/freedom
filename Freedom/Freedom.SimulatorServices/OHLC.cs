using System;

namespace Freedom.SimulatorServices
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
        public DateTime Start { get; set; }
        public DateTime End { get; set; }
        public double Volume { get; set; }
    }
}