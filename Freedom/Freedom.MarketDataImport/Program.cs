using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Freedom.Backtesting;
using System.Data.SqlClient;
using System.Globalization;

namespace Freedom.MarketDataImport
{
    class Program
    {
        static void Main(string[] args)
        {
            //Import from Coinigy files
            var ohlcList = Extract("Data\\1065_CXIO_BTCEUR_20170630_20171102.csv");

            //Upload it to the SQL Server on Azure 
            Load(ohlcList);
        }

        public static List<OHLC> Extract(string fileName)
        {
            var lines = File.ReadLines(fileName);
            var ohlcList = new List<OHLC>();

            decimal lastClose = 0;

            foreach (var line in lines)
            {
                var data = line.Split('\t');

                //have to replace \N with last close

                var open = data[3] == "\\N" ? lastClose : decimal.Parse(data[3]);
                var high = decimal.Parse(data[4]);
                var low = decimal.Parse(data[5]);
                var close = decimal.Parse(data[6]);

                var ohlc = new OHLC(open, high, low, close);

                ohlc.Volume = double.Parse(data[7]);
                ohlc.Start = DateTime.ParseExact(data[8], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                ohlc.End = DateTime.ParseExact(data[9], "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
                
                if(ohlc.Start > DateTime.ParseExact("2017-08-26 12:33:00", "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                    ohlcList.Add(ohlc);

                lastClose = close;
            }

            return ohlcList;
        }

        public static void Load(List<OHLC> ohlcList)
        {
            //Write the trades into a database
            SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder();
            builder.DataSource = "ff-marketdata.database.windows.net";
            builder.UserID = "marketdata";
            builder.Password = "mar20X/b";
            builder.InitialCatalog = "marketdata";

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand("", connection))
                {
                    command.CommandType = System.Data.CommandType.Text;
                    command.CommandText = "INSERT INTO dbo.OHLC (Exchange, Market, [Open], High, Low, [Close], Volume, Start, [End]) " +
                        "VALUES(@Exchange, @Market, @Open, @High, @Low, @Close, @Volume, @Start, @End)";

                    command.Parameters.Add(new SqlParameter("Exchange", ""));
                    command.Parameters.Add(new SqlParameter("Market", ""));
                    command.Parameters.Add(new SqlParameter("Open", System.Data.SqlDbType.Money));
                    command.Parameters.Add(new SqlParameter("High", System.Data.SqlDbType.Money));
                    command.Parameters.Add(new SqlParameter("Low", System.Data.SqlDbType.Money));
                    command.Parameters.Add(new SqlParameter("Close", System.Data.SqlDbType.Money));
                    command.Parameters.Add(new SqlParameter("Volume", System.Data.SqlDbType.Decimal));
                    command.Parameters.Add(new SqlParameter("Start", System.Data.SqlDbType.DateTime));
                    command.Parameters.Add(new SqlParameter("End", System.Data.SqlDbType.DateTime));
        
                    command.Parameters["Volume"].Precision = 18;
                    command.Parameters["Volume"].Scale = 8;

                    foreach (var ohlc in ohlcList)
                    {
                        command.Parameters["Exchange"].Value = "CEX.IO";
                        command.Parameters["Market"].Value = "BTC/EUR";
                        command.Parameters["Open"].Value = ohlc.Open;
                        command.Parameters["High"].Value = ohlc.High;
                        command.Parameters["Low"].Value = ohlc.Low;
                        command.Parameters["Close"].Value = ohlc.Close;
                        command.Parameters["Volume"].Value = ohlc.Volume;
                        command.Parameters["Start"].Value = ohlc.Start;
                        command.Parameters["End"].Value = ohlc.End;

                        command.ExecuteNonQuery();
                    }
                }
            }
        }
    }
}
