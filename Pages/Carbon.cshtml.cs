using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using CarbonOffset.Models;
using CarbonOffset.Services;

namespace CarbonOffset.Pages
{
    [BindProperties(SupportsGet=true)]
    public class CarbonModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        // Api Key for ICAO Data API
        private readonly string _icaoApiKey = "758ccce0-b9e2-11e9-a385-7b7ddcc61eff"; //aa718320-b6c1-11e9-bd0e-1bc044c2888a;
        private FlightDetails _departureFlight;
        private FlightDetails _returnFlight;

        public string DepartureAircraftName { get; set; }
        public string DepartureFlightNumber { get; set; }
        public string ReturnAircraftName { get; set; }
        public string ReturnFlightNumber { get; set; }
        public DateTime DepartureDate { get; set; }
        public DateTime ReturnDate { get; set; }
        public FlightClassType ClassType { get; set; }
        public int Passengers { get; set; }
        public string OriginAirport { get; set; }
        public string DestinationAirport { get; set; }


        public CarbonProjectDetails CarbonProjectDetails { get; set; }
        public List<CarbonProjectDetails> CarbonProjects { get; set; }

        // Setting Http Client factory
        public CarbonModel(IHttpClientFactory clientFactory, CarbonFlightOffsetService carbonFlightOffsetService)
        {
            if (clientFactory != null)
            {
                _clientFactory = clientFactory;
            }
            if (carbonFlightOffsetService != null)
            {
                _carbonFlightOffsetService = carbonFlightOffsetService;
            }
            if (Globals.Airports.Count == 0)
            {
                Globals.LoadAirports("./Data/airports.json");
            }
            if (Globals.GetAircraftCount() == 0)
            {
                Globals.LoadAircrafts("./Data/aircrafts.json");
            }
            if (Globals.IataToIcaoAirportCodes.Count == 0)
            {
                Globals.LoadAirportCode("./Data/IATA_ICAO.csv");
            }
        }

        public IActionResult OnGet()
        {
            if (String.IsNullOrEmpty(DepartureAircraftName) ||
                String.IsNullOrEmpty(DepartureFlightNumber) ||
                String.IsNullOrEmpty(OriginAirport) ||
                String.IsNullOrEmpty(DestinationAirport))
            {
                return RedirectToPage("/Index");
            }

            _departureFlight = new FlightDetails()
            {
                AircraftName = DepartureAircraftName,
                FlightNumber = DepartureFlightNumber,
                Date = DepartureDate,
                ClassType = ClassType,
                CurrentCapacity = Passengers,
                IataOriginAirportCode = OriginAirport,
                IcaoOriginAirportCode = Globals.IataToIcaoAirportCodes[OriginAirport],
                IataDestinationAirportCode = DestinationAirport,
                IcaoDestinationAirportCode = Globals.IataToIcaoAirportCodes[DestinationAirport]
            };
            PopulateCarbonEmissionDetails(ref _departureFlight);
            ViewData["CurrentCarbonEmission"] = _departureFlight.CurrentCarbonEmission;
            ViewData["TotalCarbonEmission"] = _departureFlight.TotalCarbonEmission;
            ViewData["Distance"] = _departureFlight.Distance;
            ViewData["FuelBurn"] = _departureFlight.FuelBurn;
            ViewData["departureFlightDetailsJson"] = JsonConvert.SerializeObject(_departureFlight);

            if (!String.IsNullOrEmpty(ReturnFlightNumber))
            {
                _returnFlight = new FlightDetails()
                {
                    AircraftName = ReturnAircraftName,
                    FlightNumber = ReturnFlightNumber,
                    Date = ReturnDate,
                    ClassType = ClassType,
                    CurrentCapacity = Passengers,
                    IataOriginAirportCode = DestinationAirport,
                    IcaoOriginAirportCode = Globals.IataToIcaoAirportCodes[DestinationAirport],
                    IataDestinationAirportCode = OriginAirport,
                    IcaoDestinationAirportCode = Globals.IataToIcaoAirportCodes[OriginAirport]
                };

                // Get carbon emission for round trip
                PopulateCarbonEmissionDetails(ref _returnFlight);
                ViewData["CurrentCarbonEmission"] = _departureFlight.CurrentCarbonEmission + _returnFlight.CurrentCarbonEmission;
                ViewData["TotalCarbonEmission"] = _departureFlight.TotalCarbonEmission + _returnFlight.TotalCarbonEmission;
                ViewData["Distance"] = _departureFlight.Distance + _returnFlight.Distance;
                ViewData["FuelBurn"] = _departureFlight.FuelBurn + _returnFlight.FuelBurn;
                ViewData["returnFlightDetailsJson"] = JsonConvert.SerializeObject(_returnFlight);
            }

            GetCarbonProjects("./Data/marketplace.json", "SGD");
            return Page();
        }

        public IActionResult OnPost()
        {
            _departureFlight = JsonConvert.DeserializeObject<FlightDetails>(Request.Form["departureFlightDetailsJson"]);
            CarbonProjectDetails = JsonConvert.DeserializeObject<CarbonProjectDetails>(Request.Form["submit"]);
            FlightOffset confirmedDepartureFlightOffset = ConfirmFlightOffset(new FlightOffset(_departureFlight, CarbonProjectDetails));
            if (Request.Form.ContainsKey("returnFlightDetailsJson"))
            {
                _returnFlight = JsonConvert.DeserializeObject<FlightDetails>(Request.Form["returnFlightDetailsJson"]);
                FlightOffset confirmedReturnFlightOffset = ConfirmFlightOffset(new FlightOffset(_returnFlight, CarbonProjectDetails));
                return RedirectToPage("/ThankYou", new
                {
                    DepartureFlightOffsetId = confirmedDepartureFlightOffset.Id,
                    ReturnFlightOffsetId = confirmedReturnFlightOffset.Id
                });
            }
            return RedirectToPage("/ThankYou", new { DepartureFlightOffsetId = confirmedDepartureFlightOffset.Id });
        }

        private void PopulateCarbonEmissionDetails(ref FlightDetails flightDetails)
        {
            List<FlightDetails> flightCarbonEmissionDetails = GetCarbonEmissions(flightDetails);
            if (flightCarbonEmissionDetails == null)
            {
                flightCarbonEmissionDetails = GetCarbonEmissions("./Data/carbonemissions.json");
            }
            foreach (FlightDetails flightCarbonEmission in flightCarbonEmissionDetails)
            {
                Aircraft aircraft = Globals.FindAircraft(flightDetails.AircraftName);
                // Get total carbon emission for aircraft base on the number of seats in each class type
                if (aircraft.F != 0)
                {
                    flightDetails.TotalCarbonEmission += flightCarbonEmission.CurrentCarbonEmission * aircraft.F;
                }
                if (aircraft.J != 0)
                {
                    flightDetails.TotalCarbonEmission += flightCarbonEmission.CurrentCarbonEmission * aircraft.J;
                }
                if (aircraft.S != 0)
                {
                    flightDetails.TotalCarbonEmission += flightCarbonEmission.CurrentCarbonEmission * aircraft.S;
                }
                // Carbon emission is the same for Reserve and Economy class type
                flightDetails.TotalCarbonEmission += flightCarbonEmission.CurrentCarbonEmission * (aircraft.Y + aircraft.R);

                // Get current carbon emission base on tickets bought
                flightDetails.CurrentCarbonEmission += flightCarbonEmission.CurrentCarbonEmission * flightDetails.CurrentCapacity;

                // Get carbon emission base on class type bought
                if (flightDetails.ClassType != FlightClassType.Economy)
                {
                    flightDetails.CurrentCarbonEmission *= 2;
                }
                flightDetails.FuelBurn += flightCarbonEmission.FuelBurn;
                flightDetails.Distance += flightCarbonEmission.Distance;
            }
        }

        // Carbon emission calculator methodology from https://www.icao.int/environmental-protection/CarbonOffset/Documents/Methodology%20ICAO%20Carbon%20Calculator_v10-2017.pdf
        private List<FlightDetails> GetCarbonEmissions(FlightDetails flightDetails)
        {
            List<FlightDetails> results = new List<FlightDetails>();

            using (HttpClient httpClient = _clientFactory.CreateClient("icaoCarbonEmissions"))
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "?api_key=" + _icaoApiKey
                + "&from=" + flightDetails.IcaoOriginAirportCode
                + "&to=" + flightDetails.IcaoDestinationAirportCode
                + "&travelclass=" + ((char)flightDetails.ClassType).ToString()
                + "&indicator=" + ((flightDetails.IsMetric) ? "m" : "s")))
            {
                string carbonEmissionJsonResult = httpClient.GetStringAsync(request.RequestUri).Result;
                results = JsonConvert.DeserializeObject<List<FlightDetails>>(carbonEmissionJsonResult);
            }

            return results;
        }

        private List<FlightDetails> GetCarbonEmissions(string filePath)
        {
            List<FlightDetails> results = new List<FlightDetails>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                results = JsonConvert.DeserializeObject<List<FlightDetails>>(reader.ReadToEnd());
            }

            return results;
        }

        private void GetCarbonProjects(string filePath, string baseCurrency)
        {
            Dictionary<string, double> exchangeRates = GetCurrencyExchangeRates(baseCurrency);

            using (StreamReader reader = new StreamReader(filePath))
            {  
                JObject jsonFile = JObject.Parse(reader.ReadToEnd());
                IList<JToken> jsonProjects = jsonFile["projects"].Children().ToList();
                CarbonProjects = new List<CarbonProjectDetails>(jsonProjects.Count);
                foreach (JToken project in jsonProjects)
                {;
                    CarbonProjectDetails carbonProject = project.ToObject<CarbonProjectDetails>();
                    // Convert Project currency to base currency
                    if (carbonProject.Currency != baseCurrency)
                    {
                        carbonProject.CarbonPrice = exchangeRates[carbonProject.Currency] / carbonProject.CarbonPrice;
                        carbonProject.Currency = baseCurrency;
                    }
                    else
                    {
                        carbonProject.CarbonPrice = carbonProject.CarbonPrice;
                    }

                    CarbonProjects.Add(carbonProject);
                }
            }
        }

        private Dictionary<string, double> GetCurrencyExchangeRates(string currencyBase)
        {
            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, "")
            {
                Content = new FormUrlEncodedContent(new[]
                {
                    new KeyValuePair<string, string>("base", currencyBase)
                })
            })
            using (HttpClient httpClient = _clientFactory.CreateClient("exchangeRate"))
            {
                string currencyExchangeRateJson = httpClient.GetStringAsync(request.RequestUri).Result;
                JObject exchangeRatesJson = JObject.Parse(currencyExchangeRateJson);
                return exchangeRatesJson["rates"].Children<JProperty>().ToDictionary( k => k.Name, v => v.Value.ToObject<double>() );
            }
        }

        private FlightOffset ConfirmFlightOffset(FlightOffset confirmFlightOffset)
        {
            FlightOffset flightOffset = _carbonFlightOffsetService.Get(confirmFlightOffset.FlightDetails);
            if (flightOffset == null)
            {
                _carbonFlightOffsetService.Create(confirmFlightOffset);
                flightOffset = _carbonFlightOffsetService.Get(confirmFlightOffset.FlightDetails);
            }
            else
            {
                Aircraft aircraft = Globals.FindAircraft(confirmFlightOffset.FlightDetails.AircraftName);
                if ((aircraft.Total - flightOffset.FlightDetails.CurrentCapacity - confirmFlightOffset.FlightDetails.CurrentCapacity) > 0)
                {
                    flightOffset.FlightDetails.CurrentCapacity += confirmFlightOffset.FlightDetails.CurrentCapacity;
                    flightOffset.FlightDetails.CurrentCarbonEmission += confirmFlightOffset.FlightDetails.CurrentCarbonEmission;
                    _carbonFlightOffsetService.Update(flightOffset.Id, flightOffset);
                }
            }

            return flightOffset;
        }
    }
}