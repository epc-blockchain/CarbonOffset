using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CarbonOffset.Models
{
    public class Airport
    {
        [JsonProperty("airportCode")]
        public string Code { get; set; }
        [JsonProperty("airportName")]
		public string Name { get; set; }
		public string CityCode { get; set; }
		public string CityName { get; set; }
		public string CountryCode { get; set; }
		public string CountryName { get; set; }
        [JsonProperty("isSQGtwyFlg")]
        public bool SiaDirect { get; set; }
        [JsonProperty("isCIBOrigin")]
        public bool SiaSiteOrigin { get; set; }
        [JsonProperty("isCIBDestination")]
        public bool SiaSiteDestination { get; set; }
        public List<string> ExcludedCountries { get; set; }
    }
}