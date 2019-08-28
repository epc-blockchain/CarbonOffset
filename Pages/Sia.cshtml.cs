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
        private readonly ISiaDestinationApiService _siaDestinationApiService;
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        [BindProperty(SupportsGet=true)]
        public String Id { get; set; }
        public List<FlightOffset> Flights { get; set; } = new List<FlightOffset>();
        public Dictionary<string, Airport> Airports { get; private set; }

        // Setting Http Client factory and MongoDB Service
        public SiaModel(ISiaDestinationApiService siaDestinationApiService, CarbonFlightOffsetService carbonFlightOffsetService)
        {
            _siaDestinationApiService = siaDestinationApiService;
            _carbonFlightOffsetService = carbonFlightOffsetService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Airports = await _siaDestinationApiService.GetAirports();

            if (String.IsNullOrEmpty(Id))
            {
                Flights = _carbonFlightOffsetService.Get();
            }
            else
            {
                Flights.Add(_carbonFlightOffsetService.Get(Id));
            }

            return Page();
        }
    }
}