using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CarbonOffset.Models;
using CarbonOffset.Services;

namespace CarbonOffset.Pages
{
    public class SiaModel : PageModel
    {
        private readonly IHttpClientFactory _clientFactory;
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        [BindProperty(SupportsGet=true)]
        public String Id { get; set; }
        public List<FlightOffset> Flights { get; set; } = new List<FlightOffset>();

        // Setting Http Client factory and MongoDB Service
        public SiaModel(IHttpClientFactory clientFactory, CarbonFlightOffsetService carbonFlightOffsetService)
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
        }
        public void OnGet()
        {
            if (String.IsNullOrEmpty(Id))
            {
                Flights = _carbonFlightOffsetService.Get();
            }
            else
            {
                Flights.Add(_carbonFlightOffsetService.Get(Id));
            }
        }
    }
}