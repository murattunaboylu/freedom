//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Freedom.DataAccessLayer
{
    using System;
    using System.Collections.Generic;
    
    public partial class Trade
    {
        public string Market { get; set; }
        public string TradeId { get; set; }
        public decimal Price { get; set; }
        public decimal Quantity { get; set; }
        public decimal Total { get; set; }
        public System.DateTime Time { get; set; }
        public string Type { get; set; }
        public int Id { get; set; }
        public string Exchange { get; set; }
    }
}
