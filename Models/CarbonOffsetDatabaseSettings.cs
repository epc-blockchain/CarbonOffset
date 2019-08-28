using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarbonOffset.Models
{
    public class CarbonOffsetDatabaseSettings
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string FlightCollectionName { get; set; }
    }
}