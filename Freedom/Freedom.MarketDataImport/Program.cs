using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Freedom.Backtesting;

namespace Freedom.MarketDataImport
{
    class Program
    {
        static void Main(string[] args)
        {
            //Import from Coinigy files
            var ohlcList = Import("Data\\MINUTE_BITF_BTCUSD_20160401_20160404.csv");

            //Upload it to the SQL Server on Azure 

        }

        public static List<OHLC> Import(string fileName)
        {
            var lines = File.ReadLines(fileName);
            var ohlcList = new List<OHLC>();

            foreach (var line in lines)
            {
                var data = line.Split('\t');

                //have to replace \N with NA?

                var open = decimal.Parse(data[3]);
                var high = decimal.Parse(data[4]);
                var low = decimal.Parse(data[5]);
                var close = decimal.Parse(data[6]);

                var ohlc = new OHLC(open, high, low, close);

                ohlcList.Add(ohlc);
            }

            return ohlcList;
        }
    }
}
