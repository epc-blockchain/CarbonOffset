using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public interface ICarbonFlightOffsetService
    {
        FlightOffset Create(FlightOffset flightOffset);
        List<FlightOffset> GetAll();
        FlightOffset Get(string id);
        FlightOffset Get(FlightDetails flightDetails);
        void Update(string id, FlightOffset carbonOffsetIn);
        void Remove(FlightOffset flightOffsetIn);
        void Remove(string id);
        void Remove(FlightDetails flightDetailsIn);
    }

    public class CarbonFlightOffsetService : ICarbonFlightOffsetService
    {
        private readonly IMongoCollection<FlightOffset> _flightOffsets;

        public CarbonFlightOffsetService(IOptions<CarbonOffsetDatabaseSettings> settings)
        {
            _flightOffsets = new MongoClient(settings.Value.ServerName).GetDatabase(settings.Value.DatabaseName).GetCollection<FlightOffset>(settings.Value.FlightCollectionName);
        }

        public List<FlightOffset> GetAll() =>
            _flightOffsets.Find(carbonOffset => true).ToList();

        public FlightOffset Get(string id) =>
            _flightOffsets.Find<FlightOffset>(flightOffset => flightOffset.Id == id).FirstOrDefault();

        public FlightOffset Get(FlightDetails flightDetails) =>
            _flightOffsets.Find<FlightOffset>(flightOffset => flightOffset.FlightDetails.Equals(flightDetails)).FirstOrDefault();

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
            _flightOffsets.DeleteOne(carbonOffset => carbonOffset.FlightDetails.Equals(flightDetailsIn));
    }
}
