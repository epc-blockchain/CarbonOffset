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
        private readonly CarbonFlightOffsetService _carbonFlightOffsetService;

        [BindProperty(SupportsGet = true)]
        public string DepartureFlightOffsetId { get; set; }
        [BindProperty(SupportsGet = true)]
        public string ReturnFlightOffsetId { get; set; }

        public FlightOffset DepartureFlightOffset { get; set; }
        public FlightOffset ReturnFlightOffset { get; set; }
        public CarbonProjectDetails CarbonProjectDetails { get; set; }

        public ThankYouModel(CarbonFlightOffsetService carbonFlightOffsetService)
        {
            if (carbonFlightOffsetService != null)
            {
                _carbonFlightOffsetService = carbonFlightOffsetService;
            }
            if (Globals.Airports.Count == 0)
            {
                Globals.LoadAirports("./Data/airports.json");
            }
        }

        public IActionResult OnGet()
        {
            if (String.IsNullOrEmpty(DepartureFlightOffsetId))
            {
                return RedirectToPage("/Index");
            }

            DepartureFlightOffset = _carbonFlightOffsetService.Get(DepartureFlightOffsetId);
            CarbonProjectDetails = DepartureFlightOffset.CarbonProjects.LastOrDefault<CarbonProjectDetails>();
            ViewData["CarbonOffset"] = Math.Round(DepartureFlightOffset.FlightDetails.CurrentCarbonEmission, 2);
            ViewData["CarbonPrice"] = Math.Round(DepartureFlightOffset.FlightDetails.CurrentCarbonEmission / 1000 * CarbonProjectDetails.CarbonPrice, 2);

            if (!String.IsNullOrEmpty(ReturnFlightOffsetId))
            {
                ReturnFlightOffset = _carbonFlightOffsetService.Get(ReturnFlightOffsetId);
                ViewData["CarbonOffset"] = Math.Round((DepartureFlightOffset.FlightDetails.CurrentCarbonEmission + ReturnFlightOffset.FlightDetails.CurrentCarbonEmission), 2);
                ViewData["CarbonPrice"] = Math.Round((DepartureFlightOffset.FlightDetails.CurrentCarbonEmission + ReturnFlightOffset.FlightDetails.CurrentCarbonEmission) / 1000 * CarbonProjectDetails.CarbonPrice, 2);

            }
            
            return Page();
        }
    }
}