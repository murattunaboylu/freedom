using System;
using System.Runtime.Serialization;

namespace Freedom.Backtesting
{
    /// <summary>
    /// {"type":"buy","date":"1510327514","amount":"0.32840474","price":"6028.9324","tid":"1471530"}
    /// </summary>
    [DataContract]
    class Trade
    {
        [DataMember(Name = "tid")]
        public int Id { get; set; }

        [DataMember(Name = "type")]
        public string Type { get; set; }

        [DataMember(Name = "date")]
        public long Seconds { get; set; }

        public DateTime Date { get; set;}

        [DataMember(Name = "amount")]
        public decimal Amount { get; set; }

        [DataMember(Name = "price")]
        public decimal Price { get; set; }

        public override string ToString()
        {
            return $"{Date:HH:mm:ss} {Type} BTC/EUR {Amount} @ {Price} id:{Id}";
        }
    }
}
