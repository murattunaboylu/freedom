using System;
using System.Linq;

namespace Freedom.SimulatorServices.Controllers
{
    public class Event
    {
        public Event(DateTime _date, string _text, string _description)
        {
            //Adding one minute to make the hour round
            //Otherwise events do not show up on close inspection 
            date = _date.AddMinutes(1).ToString("ddd MMM dd yyyy HH:mm:ss");
            type = "sign";
            graph = "graph1";
            backgroundColor = "#85CDE6";
            text = _text.First().ToString();
            description = _text + " " + _description;
        }

        public string date { get; set; }
        public string type { get; set; }
        public string backgroundColor { get; set; }
        public string graph { get; set; }
        public string text { get; set; }
        public string description { get; set; }
    }
}