using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CarbonOffset.Models
{
    public class Aircraft
    {
        [JsonProperty("aircraftName")]
        public string Name { get; set; }
        public string Icao { get; set; }
        public string Iata { get; set; }
        public int R { get; set; }
        public int F { get; set; }
        public int J { get; set; }
        public int S { get; set; }
        public int Y { get; set; }
        public int Total { get; set; }
    }
}
