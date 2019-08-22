using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using Newtonsoft.Json;

namespace CarbonOffset.Models
{
    public class FlightOffset
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public FlightDetails FlightDetails { get; set; }
        public List<CarbonProjectDetails> CarbonProjects { get; set; } = new List<CarbonProjectDetails>();

        public FlightOffset(FlightDetails flightDetails, CarbonProjectDetails carbonProjectDetails)
        {
            FlightDetails = flightDetails;
            CarbonProjects.Add(carbonProjectDetails);
        }
    }
}