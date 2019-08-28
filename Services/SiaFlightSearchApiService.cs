using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public class SiaFlightSearchApiService
    {
        private readonly HttpClient _client;

        public SiaFlightSearchApiService(HttpClient client, IConfiguration configuration)
        {
            client.BaseAddress = new Uri(configuration.GetConnectionString("SiaApi") + configuration["SiaFlightSearchApiSettings:Endpoint"]);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("apikey", configuration["SiaFlightSearchApiSettings:Key"]);
            _client = client;
        }

        public async Task<List<FlightDetails>> GetSiaFlightSearch(string originAirport, string destinationAirport, DateTime departureDate, DateTime returnDate, FlightClassType classType, int passengers)
        {
            List<FlightDetails> result = new List<FlightDetails>();

            // Generate JSON input data for API call
            StringBuilder sb = new StringBuilder();
            using (JsonWriter writer = new JsonTextWriter(new StringWriter(sb)))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("clientUUID");
                writer.WriteValue(Guid.NewGuid().ToString());
                writer.WritePropertyName("request");
                writer.WriteStartObject();
                writer.WritePropertyName("itineraryDetails");
                writer.WriteStartArray();
                writer.WriteStartObject();
                writer.WritePropertyName("originAirportCode");
                writer.WriteValue(originAirport);
                writer.WritePropertyName("destinationAirportCode");
                writer.WriteValue(destinationAirport);
                writer.WritePropertyName("departureDate");
                writer.WriteValue(departureDate.ToString("yyyy-MM-dd"));
                if (returnDate != new DateTime())
                {
                    writer.WritePropertyName("returnDate");
                    writer.WriteValue(returnDate.ToString("yyyy-MM-dd"));
                }
                writer.WriteEndObject();
                writer.WriteEndArray();
                writer.WritePropertyName("cabinClass");
                writer.WriteValue(((char)classType).ToString());
                writer.WritePropertyName("adultCount");
                writer.WriteValue(passengers);
                writer.WritePropertyName("flightSortingRequired");
                writer.WriteValue(true);
                writer.WriteEndObject();
                writer.WriteEndObject();
                writer.Close();
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json")
            };

            using (var response = await _client.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResults = JObject.Parse(await response.Content.ReadAsStringAsync());

                    // Check for errors from API.
                    if (jsonResults.TryGetValue("message", out JToken message) && !String.IsNullOrEmpty(message.Value<string>()))
                    {
                        return null;
                    }
                    else
                    {
                        // Flights found, get flight number and aircraft model for seat capacity.
                        if (jsonResults["response"]["segments"] == null)
                        {
                            FlightDetails departureFlight = new FlightDetails();
                            IList<JToken> flights = jsonResults["response"]["flights"].Children().ToList();

                            // Get first flight from result for departure
                            result.Add(new FlightDetails()
                            {
                                CurrentCapacity = passengers,
                                ClassType = classType,
                                IataOriginAirportCode = originAirport,
                                IataDestinationAirportCode = destinationAirport,
                                //IcaoOriginAirportCode = Globals.Airports[originAirport].Code,
                                //IcaoDestinationAirportCode = Globals.Airports[destinationAirport].Code,
                                Date = flights[0]["segments"][0].Value<DateTime>("departureDateTime"),
                                FlightNumber = flights[0]["segments"][0]["legs"][0]["operatingAirline"].Value<string>("code") + jsonResults["response"]["flights"][0]["segments"][0]["legs"][0].Value<string>("flightNumber"),
                                AircraftName = flights[0]["segments"][0]["legs"][0]["aircraft"].Value<string>("name"),
                            });

                            // Get first flight from result for return
                            if (flights.Count > 1)
                            {
                                result.Add(new FlightDetails()
                                {
                                    CurrentCapacity = passengers,
                                    ClassType = classType,
                                    IataOriginAirportCode = destinationAirport,
                                    IataDestinationAirportCode = originAirport,
                                    //IcaoOriginAirportCode = Globals.Airports[destinationAirport].Code,
                                    //IcaoDestinationAirportCode = Globals.Airports[originAirport].Code,
                                    Date = flights[1]["segments"][0].Value<DateTime>("departureDateTime"),
                                    FlightNumber = flights[1]["segments"][0]["legs"][0]["operatingAirline"].Value<string>("code") + jsonResults["response"]["flights"][1]["segments"][0]["legs"][0].Value<string>("flightNumber"),
                                    AircraftName = flights[1]["segments"][0]["legs"][0]["aircraft"].Value<string>("name"),
                                });
                            }
                        }
                    }
                }
                else
                {
                    return null;
                }
            }

            return result;
        }
    }
}