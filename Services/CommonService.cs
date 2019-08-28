using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public interface ICommonService
    {
        Dictionary<string, string> GetIataIcaoAirportCodes();
        Dictionary<string, Aircraft> GetAircrafts();
        List<CarbonProjectDetails> GetCarbonProjects();
        Dictionary<string, double> GetCurrencyExchangeRates(string currencyBase);
    }

    public class CommonService : ICommonService
    {
        private readonly HttpClient _client;
        private readonly IMemoryCache _cache;

        public CommonService(IMemoryCache cache, HttpClient client, IConfiguration configuration)
        {
            _cache = cache;
            client.BaseAddress = new Uri(configuration.GetConnectionString("CurrencyExchangeRateApi"));
            _client = client;
        }

        // Get Iata to Icao airport codes.
        // Database extracted from airports.dat at https://openflights.org/data.html.
        public Dictionary<string, string> GetIataIcaoAirportCodes()
        {
            if (_cache.Get("AirportCodes") == null)
            {
                Dictionary<string, string> airportCodes = new Dictionary<string, string>();
                string[] lines = File.ReadAllLines("./Data/IATA_ICAO.csv");
                foreach (string line in lines)
                {
                    string[] listItem = line.Split(',');
                    airportCodes.Add(listItem[0], listItem[1]);
                }
                _cache.Set("AirportCodes", airportCodes);
            }

            return _cache.Get<Dictionary<string, string>>("AirportCodes");
        }

        // Get SIA Aircraft models.
        // Information from https://www.singaporeair.com/en_UK/my/flying-withus/our-story/our-fleet/, https://en.wikipedia.org/wiki/Singapore_Airlines_fleet and https://en.wikipedia.org/wiki/SilkAir#Fleet.
        // Iata and Icao code from https://en.wikipedia.org/wiki/List_of_aircraft_type_designators.
        public Dictionary<string, Aircraft> GetAircrafts()
        {
            if (_cache.Get("AircraftList") == null)
            {
                Dictionary<string, Aircraft> aircrafts = new Dictionary<string, Aircraft>();
                using (StreamReader reader = new StreamReader("./Data/aircrafts.json"))
                {
                    JObject jsonFile = JObject.Parse(reader.ReadToEnd());
                    aircrafts = jsonFile["data"]["aircraftList"].Children<JObject>().ToDictionary(k => ((Aircraft)k.ToObject<Aircraft>()).Name, v => (Aircraft)v.ToObject<Aircraft>());
                }

                _cache.Set("AircraftList", aircrafts);
            }

            return _cache.Get<Dictionary<string, Aircraft>>("AircraftList");
        }

        // Get Carbon projects from BESC Marketplace
        // Marketplace information currently from file instead of actual API service from BESC platform.
        public List<CarbonProjectDetails> GetCarbonProjects()
        {
            if (_cache.Get("CarbonProjects") == null)
            {
                using (StreamReader reader = new StreamReader("./Data/marketplace.json"))
                {
                    JObject projects = JObject.Parse(reader.ReadToEnd());
                    _cache.Set("CarbonProjects", projects["projects"].Select(t => t.ToObject<CarbonProjectDetails>()).ToList());
                }
            }

            return _cache.Get<List<CarbonProjectDetails>>("CarbonProjects");
        }

        // Get Currency Exchange rates against base currency from API service.
        public Dictionary<string, double> GetCurrencyExchangeRates(string currencyBase)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("base", currencyBase)
                })
            })
            {
                string currencyExchangeRateJson = _client.GetStringAsync(request.RequestUri).Result;
                JObject exchangeRatesJson = JObject.Parse(currencyExchangeRateJson);
                return exchangeRatesJson["rates"].Children<JProperty>().ToDictionary(k => k.Name, v => v.Value.ToObject<double>());
            }
        }
    }
}