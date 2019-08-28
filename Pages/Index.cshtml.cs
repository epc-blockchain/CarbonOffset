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
    public class IndexModel : PageModel
    {
        private readonly ISiaDestinationApiService _siaDestinationApiService;
        private readonly SiaFlightSearchApiService _siaFlightSearchApiService;

        public string OriginAirport { get; set; }
        public string DestinationAirport { get; set; }
        public DateTime DepartureDate { get; set; } = DateTime.Today;
        public DateTime ReturnDate { get; set; }
        public FlightClassType ClassType { get; set; } = FlightClassType.Economy;
        public int Passengers { get; set; } = 1;
        public List<SelectListItem> OriginAirports { get; set; }
        public List<SelectListItem> DestinationAirports { get; set; }

        public IndexModel(ISiaDestinationApiService siaDestinationApiService, SiaFlightSearchApiService siaFlightSearchApiService)
        {
            _siaDestinationApiService = siaDestinationApiService;
            _siaFlightSearchApiService = siaFlightSearchApiService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            List<Airport> airports = await _siaDestinationApiService.GetOriginAirports(OriginAirport);
            OriginAirports = new List<SelectListItem>(airports.Select(airport => new SelectListItem(airport.CityName + " ," + airport.CountryName + " (" + airport.Name + ")", airport.Code)));
            airports = await _siaDestinationApiService.GetDestinationAirports(OriginAirport, DestinationAirport);
            DestinationAirports = new List<SelectListItem>(airports.Select(airport => new SelectListItem(airport.CityName + " ," + airport.CountryName + " (" + airport.Name + ")", airport.Code)));
            return Page();
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

            List<FlightDetails> siaFlightSearch = await _siaFlightSearchApiService.GetSiaFlightSearch(OriginAirport, DestinationAirport, DepartureDate, ReturnDate, ClassType, Passengers);
            if (siaFlightSearch == null)
            {
                ModelState.AddModelError("ErrorSearch", "No flights found on the specific dates");
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
    }
}