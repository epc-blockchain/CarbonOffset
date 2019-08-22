using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace CarbonOffset.Models
{
    //https://developer.singaporeair.com/docs/flight_search/flightavailability
    public enum FlightClassType
    {
        Economy = 'Y',
        Premium = 'S',
        Business = 'J',
        First = 'F'
    }

    public class FlightDetails
    {
        public string FlightNumber { get; set; }
        public string AircraftName { get; set; }
        public string IataOriginAirportCode { get; set; }
        public string IataDestinationAirportCode { get; set; }
        [JsonProperty("from_airport_code")]
        public string IcaoOriginAirportCode { get; set; }
        [JsonProperty("to_airport_code")]
        public string IcaoDestinationAirportCode { get; set; }
        [DataType(DataType.Date)]
        public DateTime Date { get; set; }
        public FlightClassType ClassType { get; set; } = FlightClassType.Economy;
        public int CurrentCapacity { get; set; } = 1;
        [JsonProperty("distance")]
        public double Distance { get; set; }
        public bool IsMetric { get; set; } = true;
        [JsonProperty("co2_kg")]
        public double CurrentCarbonEmission { get; set; }
        public double TotalCarbonEmission { get; set; }
        [JsonProperty("fuelburn_kg")]
        public double FuelBurn { get; set; }

        public override bool Equals(Object obj)
        {
            //Check for null and compare run-time types.
            if ((obj == null) || !this.GetType().Equals(obj.GetType()))
            {
                return false;
            }
            else
            {
                string json = JsonConvert.SerializeObject((FlightDetails)obj, Formatting.Indented);
                return (json == JsonConvert.SerializeObject(this, Formatting.Indented));
            }
        }
    }
}