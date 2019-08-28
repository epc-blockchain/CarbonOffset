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
    [BindProperties(SupportsGet = true)]
    public class CarbonModel : PageModel
    {
        private readonly ICommonService _commonService;
        private readonly IcaoCarbonEmissionApiService _icaoCarbonEmissionApiService;
        private readonly ISiaDestinationApiService _siaDestinationApiService;
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        private FlightDetails _departureFlight;
        private FlightDetails _returnFlight;

        public Dictionary<string, Airport> Airports { get; private set; }
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
        public CarbonModel(ICommonService commonService, IcaoCarbonEmissionApiService icaoCarbonEmissionApiService, ISiaDestinationApiService siaDestinationApiService, CarbonFlightOffsetService carbonFlightOffsetService)
        {
            _commonService = commonService;
            _icaoCarbonEmissionApiService = icaoCarbonEmissionApiService;
            _siaDestinationApiService = siaDestinationApiService;
            _carbonFlightOffsetService = carbonFlightOffsetService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (String.IsNullOrEmpty(DepartureAircraftName) ||
                String.IsNullOrEmpty(DepartureFlightNumber) ||
                String.IsNullOrEmpty(OriginAirport) ||
                String.IsNullOrEmpty(DestinationAirport))
            {
                return RedirectToPage("/Index");
            }

            Airports = await _siaDestinationApiService.GetAirports();

            _departureFlight = new FlightDetails()
            {
                AircraftName = DepartureAircraftName,
                FlightNumber = DepartureFlightNumber,
                Date = DepartureDate,
                ClassType = ClassType,
                CurrentCapacity = Passengers,
                IataOriginAirportCode = OriginAirport,
                IcaoOriginAirportCode = _commonService.GetIataIcaoAirportCodes()[OriginAirport],
                IataDestinationAirportCode = DestinationAirport,
                IcaoDestinationAirportCode = _commonService.GetIataIcaoAirportCodes()[DestinationAirport]
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
                    IcaoOriginAirportCode = _commonService.GetIataIcaoAirportCodes()[DestinationAirport],
                    IataDestinationAirportCode = OriginAirport,
                    IcaoDestinationAirportCode = _commonService.GetIataIcaoAirportCodes()[OriginAirport]
                };

                // Get carbon emission for round trip
                PopulateCarbonEmissionDetails(ref _returnFlight);
                ViewData["CurrentCarbonEmission"] = _departureFlight.CurrentCarbonEmission + _returnFlight.CurrentCarbonEmission;
                ViewData["TotalCarbonEmission"] = _departureFlight.TotalCarbonEmission + _returnFlight.TotalCarbonEmission;
                ViewData["Distance"] = _departureFlight.Distance + _returnFlight.Distance;
                ViewData["FuelBurn"] = _departureFlight.FuelBurn + _returnFlight.FuelBurn;
                ViewData["returnFlightDetailsJson"] = JsonConvert.SerializeObject(_returnFlight);
            }

            GetCarbonProjects("SGD");
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
            /*List<FlightDetails> flightCarbonEmissionDetails = _icaoCarbonEmissionApiService.GetCarbonEmissions(flightDetails);
            if (flightCarbonEmissionDetails == null)
            {
                flightCarbonEmissionDetails = GetCarbonEmissions("./Data/carbonemissions.json");
            }*/
            List<FlightDetails> flightCarbonEmissionDetails = GetCarbonEmissions("./Data/carbonemissions.json");
            foreach (FlightDetails flightCarbonEmission in flightCarbonEmissionDetails)
            {
                Aircraft aircraft = _commonService.GetAircrafts()[flightDetails.AircraftName];
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

        private List<FlightDetails> GetCarbonEmissions(string filePath)
        {
            List<FlightDetails> results = new List<FlightDetails>();

            using (StreamReader reader = new StreamReader(filePath))
            {
                results = JsonConvert.DeserializeObject<List<FlightDetails>>(reader.ReadToEnd());
            }

            return results;
        }

        private void GetCarbonProjects(string baseCurrency)
        {
            Dictionary<string, double> exchangeRates = _commonService.GetCurrencyExchangeRates(baseCurrency);
            CarbonProjects = _commonService.GetCarbonProjects();
            foreach (CarbonProjectDetails carbonProject in CarbonProjects)
            {
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
                if ((_commonService.GetAircrafts()[confirmFlightOffset.FlightDetails.AircraftName].Total - flightOffset.FlightDetails.CurrentCapacity - confirmFlightOffset.FlightDetails.CurrentCapacity) > 0)
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