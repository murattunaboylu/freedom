using Freedom.DataAccessLayer;

namespace Freedom.SimulatorServices
{
    public class OhlcIndicators : OHLC
    {
        public OhlcIndicators(decimal open, decimal high, decimal low, decimal close) 
        {
            Open = open;
            High = high;
            Low = low;
            Close = close;
        }

        public decimal Mva10 { get; set; }
        public decimal Mva200 { get; set; }
        public double Rsi2 { get; set; }
        public double Rsi14 { get; set; }
        public double PercentB { get; set; }
        public double Bandwidth { get; set; }

        public string Action { get; set; }

        public OhlcIndicators Clone()
        {
            return (OhlcIndicators) MemberwiseClone();
        }
    }
}