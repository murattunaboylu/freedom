using System;

namespace Freedom.SimulatorServices.Controllers
{
    public class Order
    {
        public decimal Price { get; set; }
        public double Quantity { get; set; }
        public DateTime Date { get; set; }
        public string Type { get; set; }
    }
}