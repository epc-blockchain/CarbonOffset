using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarbonOffset.Models
{
    public interface ICarbonOffsetDatabaseSettings
    {
        string ServerName { get; set; }
        string DatabaseName { get; set; }
        string FlightCollectionName { get; set; }
    }

    public class CarbonOffsetDatabaseSettings : ICarbonOffsetDatabaseSettings
    {
        public string ServerName { get; set; }
        public string DatabaseName { get; set; }
        public string FlightCollectionName { get; set; }
    }
}
