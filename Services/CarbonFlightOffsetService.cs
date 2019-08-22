using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public class CarbonFlightOffsetService
    {
        private readonly IMongoCollection<FlightOffset> _flightOffsets;

        public CarbonFlightOffsetService(ICarbonOffsetDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ServerName);
            var database = client.GetDatabase(settings.DatabaseName);

            _flightOffsets = database.GetCollection<FlightOffset>(settings.FlightCollectionName);
        }

        public List<FlightOffset> Get() =>
            _flightOffsets.Find(carbonOffset => true).ToList();

        public FlightOffset Get(string id) =>
            _flightOffsets.Find<FlightOffset>(flightOffset => flightOffset.Id == id).FirstOrDefault();

        public FlightOffset Get(FlightDetails flightDetails) =>
            _flightOffsets.Find<FlightOffset>(flightOffset => flightOffset.FlightDetails == flightDetails).FirstOrDefault();

        public FlightOffset Create(FlightOffset flightOffset)
        {
            _flightOffsets.InsertOne(flightOffset);
            return flightOffset;
        }

        public void Update(string id, FlightOffset carbonOffsetIn)
        {
            FlightOffset currentFlightOffset = Get(id);
            if (currentFlightOffset != null)
            {
                currentFlightOffset.CarbonProjects.AddRange(carbonOffsetIn.CarbonProjects);
                _flightOffsets.ReplaceOne(flightOffset => flightOffset.Id == id, currentFlightOffset);
            }
        }

        public void Remove(FlightOffset flightOffsetIn) =>
            _flightOffsets.DeleteOne(flightOffset => flightOffset.Id == flightOffsetIn.Id);

        public void Remove(string id) =>
            _flightOffsets.DeleteOne(flightOffset => flightOffset.Id == id);

        public void Remove(FlightDetails flightDetailsIn) =>
            _flightOffsets.DeleteOne(carbonOffset => carbonOffset.FlightDetails == flightDetailsIn);
    }
}
