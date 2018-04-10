using System;
using System.Collections.Generic;

namespace Freedom.SimulatorServices.Controllers
{
    public class SimulationResult
    {
        public string Description { get; set; }
        public List<decimal> Values { get; set; }
        public List<decimal> Volumes { get; set; }
        public List<double> Lower { get; set; }
        public List<double> Upper { get; set; }
        public Stats Stats { get; set; }
        public List<Order> Orders { get; set; }
        public List<DateTime> Dates { get; internal set; }
        public List<Event> Events { get; internal set; }

        public SimulationResult()
        {
            Stats = new Stats(null,null,null);
        }
    }
}