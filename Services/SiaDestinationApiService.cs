using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public interface ISiaDestinationApiService
    {
        Task<Dictionary<string, Airport>> GetAirports();
        Task<List<Airport>> GetOriginAirports(string searchOriginAirport = null);
        Task<List<Airport>> GetDestinationAirports(string originAirportCode = null, string searchDestinationAirport = null);
    }

    public class SiaDestinationApiService : ISiaDestinationApiService
    {
        private readonly HttpClient _client;
        private readonly IMemoryCache _cache;

        public SiaDestinationApiService(HttpClient client, IConfiguration configuration, IMemoryCache cache)
        {
            _cache = cache;
            client.BaseAddress = new Uri(configuration.GetConnectionString("SiaApi") + configuration["SiaDestinationApiSettings:Endpoint"]);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-csl-client-uuid", Guid.NewGuid().ToString());
            client.DefaultRequestHeaders.Add("api_key", configuration["SiaDestinationApiSettings:Key"]);
            using (SHA256 sha256 = SHA256.Create())
            {
                // Current unix timestamp
                int seconds = (int)(DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
                // Generate hash from API key, secret and timestamp
                byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(configuration["SiaDestinationApiSettings:Key"] + configuration["SiaDestinationApiSettings:Secret"] + seconds));
                string sigHash = GetStringFromHash(hash);
                client.DefaultRequestHeaders.Add("x-signature", sigHash);
            }
            _client = client;
        }

        public async Task<Dictionary<string, Airport>> GetAirports()
        {
            if (_cache.Get("Airports") == null)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "");
                using (var response = await _client.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        JObject jsonResult = JObject.Parse(await response.Content.ReadAsStringAsync());
                        _cache.Set("Airports", jsonResult["data"]["destinationList"].Children<JObject>().ToDictionary(k => (string)k.Properties().First().Value, v => (Airport)v.ToObject<Airport>()));
                    }
                }
            }

            return _cache.Get<Dictionary<string, Airport>>("Airports");
        }

        public async Task<List<Airport>> GetOriginAirports(string searchOriginAirport = null)
        {
            Dictionary<string, Airport> airports = await GetAirports();
            if (string.IsNullOrEmpty(searchOriginAirport))
            {
                return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteOrigin == true).ToList();
            }
            else
            {
                return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteOrigin == true &&
                        (airport.Name.Contains(searchOriginAirport) || airport.CityName.Contains(searchOriginAirport) || airport.CountryName.Contains(searchOriginAirport)))
                        .ToList();
            }
        }

        public async Task<List<Airport>> GetDestinationAirports(string originAirportCode = null, string searchDestinationAirport = null)
        {
            Dictionary<string, Airport> airports = await GetAirports();
            if (string.IsNullOrEmpty(originAirportCode))
            {
                if (string.IsNullOrEmpty(searchDestinationAirport))
                {
                    return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteDestination == true).ToList();
                }
                else
                {
                    return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteDestination == true
                            && (airport.Name.Contains(searchDestinationAirport) || airport.CityName.Contains(searchDestinationAirport) || airport.CountryName.Contains(searchDestinationAirport)))
                        .ToList();
                }
            }
            else
            {
                if (string.IsNullOrEmpty(searchDestinationAirport))
                {
                    return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteDestination == true
                            && airports.ContainsKey(originAirportCode)
                            && airports[originAirportCode].ExcludedCountries != null
                            && !airports[originAirportCode].ExcludedCountries.Contains(airport.CountryName))
                        .ToList();
                }
                else
                {
                    return airports.Select(v => v.Value)
                        .Where(airport => airport.SiaSiteDestination == true
                            && airports.ContainsKey(originAirportCode)
                            && airports[originAirportCode].ExcludedCountries != null
                            && !airports[originAirportCode].ExcludedCountries.Contains(airport.CountryName)
                            && (airport.Name.Contains(searchDestinationAirport) || airport.CityName.Contains(searchDestinationAirport) || airport.CountryName.Contains(searchDestinationAirport)))
                        .ToList();
                }
            }
        }

        // Convert byte[] of hash value to string
        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
    }
}
