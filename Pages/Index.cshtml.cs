using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CarbonOffset.Models;

namespace CarbonOffset.Pages
{
    [BindProperties(SupportsGet = true)]
    public class IndexModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;

        public string OriginAirport { get; set; }
        public string DestinationAirport { get; set; }
        public DateTime DepartureDate { get; set; } = DateTime.Today;
        public DateTime ReturnDate { get; set; }
        public FlightClassType ClassType { get; set; } = FlightClassType.Economy;
        public int Passengers { get; set; } = 1;

        public IndexModel(IHttpClientFactory clientFactory)
        {
            // Setting Http Client factory
            if (clientFactory != null)
            {
                _clientFactory = clientFactory;
            }
            // Load all airports
            if (Globals.Airports.Count == 0)
            {
                Globals.LoadAirports("./Data/airports.json");
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Ensure form inputs are not left out
            if (String.IsNullOrEmpty(OriginAirport) ||
                String.IsNullOrEmpty(DestinationAirport) ||
                String.IsNullOrEmpty(Request.Form["DepartureDate"]))
            {
                return Page();
            }

            DepartureDate = DateTime.Parse(Request.Form["DepartureDate"]);
            if (!String.IsNullOrEmpty(Request.Form["ReturnDate"]))
            {
                ReturnDate = DateTime.Parse(Request.Form["ReturnDate"]);
            }

            List<FlightDetails> siaFlightSearch = await GetSiaFlightSearch(OriginAirport, DestinationAirport, DepartureDate, ReturnDate, ClassType, Passengers);
            if (ModelState.ContainsKey("ErrorSearch"))
            {
                return Page();
            }
            if (siaFlightSearch.Count > 1)
            {
                return RedirectToPage("/Carbon", new
                {
                    ClassType,
                    Passengers,
                    OriginAirport = siaFlightSearch[0].IataOriginAirportCode,
                    DestinationAirport = siaFlightSearch[0].IataDestinationAirportCode,
                    DepartureDate = siaFlightSearch[0].Date,
                    DepartureFlightNumber = siaFlightSearch[0].FlightNumber,
                    DepartureAircraftName = siaFlightSearch[0].AircraftName,
                    ReturnDate = siaFlightSearch[1].Date,
                    ReturnFlightNumber = siaFlightSearch[1].FlightNumber,
                    ReturnAircraftName = siaFlightSearch[1].AircraftName
                });
            }
            return RedirectToPage("/Carbon", new
            {
                ClassType,
                Passengers,
                OriginAirport = siaFlightSearch[0].IataOriginAirportCode,
                DestinationAirport = siaFlightSearch[0].IataDestinationAirportCode,
                DepartureDate = siaFlightSearch[0].Date,
                DepartureFlightNumber = siaFlightSearch[0].FlightNumber,
                DepartureAircraftName = siaFlightSearch[0].AircraftName
            });
        }

        // Fetch SIA Destinations (Airports) from API.
        private async Task<Dictionary<string, Airport>> GetAirportsAsync()
        {
            Dictionary<string, Airport> result = new Dictionary<string, Airport>();
            var request = new HttpRequestMessage(HttpMethod.Post, "destinations/get");
            using (HttpClient httpClient = _clientFactory.CreateClient("siaDestinations"))
            using (var response = await httpClient.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResult = JObject.Parse(await response.Content.ReadAsStringAsync());
                    result = jsonResult["data"]["destinationList"].Children<JObject>().ToDictionary( k => (string)k.Properties().First().Value, v => (Airport)v.ToObject< Airport>());
                }
            }

            return result;
        }

        // Search SIA Flights from API
        private async Task<List<FlightDetails>> GetSiaFlightSearch(string originAirport, string destinationAirport, DateTime departureDate, DateTime returnDate, FlightClassType classType, int passengers)
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

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "v1/commercial/flightavailability/get")
            {
                Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json")
            };

            using (HttpClient httpClient = _clientFactory.CreateClient("siaFlightSearch"))
            using (var response = await httpClient.SendAsync(request))
            {
                if (response.IsSuccessStatusCode)
                {
                    JObject jsonResults = JObject.Parse(await response.Content.ReadAsStringAsync());

                    // Check for errors from API.
                    if (jsonResults.TryGetValue("message", out JToken message) && !String.IsNullOrEmpty(message.Value<string>()))
                    {
                        ModelState.AddModelError("ErrorSearch", jsonResults.Value<string>("code") + ": " + message.Value<string>());
                    }
                    else
                    {
                        // No flights found on dates chosen, set first result from calender search.
                        if (jsonResults["response"]["segments"] != null)
                        {
                            ModelState.AddModelError("ErrorSearch", "No flights found on the specific dates");
                        }

                        // Flights found, get flight number and aircraft model for seat capacity.
                        else
                        {
                            ModelState.Remove("ErrorSearch");
                            FlightDetails departureFlight = new FlightDetails();
                            IList<JToken> flights = jsonResults["response"]["flights"].Children().ToList();

                            // Get first flight from result for departure
                            result.Add(new FlightDetails()
                            {
                                CurrentCapacity = passengers,
                                ClassType = classType,
                                IataOriginAirportCode = originAirport,
                                IataDestinationAirportCode = destinationAirport,
                                IcaoOriginAirportCode = Globals.Airports[originAirport].Code,
                                IcaoDestinationAirportCode = Globals.Airports[destinationAirport].Code,
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
                                    IcaoOriginAirportCode = Globals.Airports[destinationAirport].Code,
                                    IcaoDestinationAirportCode = Globals.Airports[originAirport].Code,
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
                    // API call failed
                    ModelState.AddModelError("ErrorSearch", response.ReasonPhrase);
                }
            }

            return result;
        }
    }
}