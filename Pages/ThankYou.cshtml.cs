using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using CarbonOffset.Models;
using CarbonOffset.Services;

namespace CarbonOffset.Pages
{
    
    public class ThankYouModel : PageModel
    {
        private readonly ISiaDestinationApiService _siaDestinationApiService;
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        [BindProperty(SupportsGet = true)]
        public string DepartureFlightOffsetId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ReturnFlightOffsetId { get; set; }

        public FlightOffset DepartureFlightOffset { get; set; }
        public FlightOffset ReturnFlightOffset { get; set; }
        public CarbonProjectDetails CarbonProjectDetails { get; set; }
        public Dictionary<string, Airport> Airports { get; private set; }

        public ThankYouModel(ISiaDestinationApiService siaDestinationApiService, CarbonFlightOffsetService carbonFlightOffsetService)
        {
            _siaDestinationApiService = siaDestinationApiService;
            _carbonFlightOffsetService = carbonFlightOffsetService;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            if (String.IsNullOrEmpty(DepartureFlightOffsetId))
            {
                return RedirectToPage("/Index");
            }

            Airports = await _siaDestinationApiService.GetAirports();
            DepartureFlightOffset = _carbonFlightOffsetService.Get(DepartureFlightOffsetId);
            CarbonProjectDetails = DepartureFlightOffset.CarbonProjects.LastOrDefault<CarbonProjectDetails>();
            ViewData["CarbonOffset"] = DepartureFlightOffset.FlightDetails.CurrentCarbonEmission;
            ViewData["CarbonPrice"] = DepartureFlightOffset.FlightDetails.CurrentCarbonEmission / 1000 * CarbonProjectDetails.CarbonPrice;

            if (!String.IsNullOrEmpty(ReturnFlightOffsetId))
            {
                ReturnFlightOffset = _carbonFlightOffsetService.Get(ReturnFlightOffsetId);
                ViewData["CarbonOffset"] = DepartureFlightOffset.FlightDetails.CurrentCarbonEmission + ReturnFlightOffset.FlightDetails.CurrentCarbonEmission;
                ViewData["CarbonPrice"] = (DepartureFlightOffset.FlightDetails.CurrentCarbonEmission + ReturnFlightOffset.FlightDetails.CurrentCarbonEmission) / 1000 * CarbonProjectDetails.CarbonPrice;

            }
            
            return Page();
        }
    }
}