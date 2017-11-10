using System;
using System.Runtime.Serialization;

namespace Freedom.MarketDataCollection
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
        public long Ticks { get; set; }

        public DateTime Date => new DateTime(Ticks);

        [DataMember(Name = "amount")]
        public decimal Amount { get; set; }

        [DataMember(Name = "price")]
        public decimal Price { get; set; }
    }
}
