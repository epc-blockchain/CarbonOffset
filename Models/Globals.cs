using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace CarbonOffset.Models
{
    public static class Globals
    {
        public static Dictionary<string, string> IataToIcaoAirportCodes { get; set; } = new Dictionary<string, string>();
        private static Dictionary<string, Aircraft> Aircrafts { get; set; } = new Dictionary<string, Aircraft>();
        public static Dictionary<string, Airport> Airports { get; set; } = new Dictionary<string, Airport>();

        // Load Iata to Icao airport codes.
        // Database extracted from airports.dat at https://openflights.org/data.html.
        public static void LoadAirportCode(string filePath)
        {
            string[] lines = File.ReadAllLines(filePath);
            foreach (string line in lines)
            {
                string[] listItem = line.Split(',');
                IataToIcaoAirportCodes.Add(listItem[0], listItem[1]);
            }
        }

        // Load SIA Aircraft models.
        // Information from https://www.singaporeair.com/en_UK/my/flying-withus/our-story/our-fleet/, https://en.wikipedia.org/wiki/Singapore_Airlines_fleet and https://en.wikipedia.org/wiki/SilkAir#Fleet.
        // Iata and Icao code from https://en.wikipedia.org/wiki/List_of_aircraft_type_designators.
        public static void LoadAircrafts(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                JObject jsonFile = JObject.Parse(reader.ReadToEnd());
                Aircrafts = jsonFile["data"]["aircraftList"].Children<JObject>().ToDictionary(k => ((Aircraft)k.ToObject<Aircraft>()).Name, v => (Aircraft)v.ToObject<Aircraft>());
            }
        }

        // Load SIA Airports from file.
        // Airports in file was retrieved via API from https://developer.singaporeair.com/docs/read/destination/Destination.
        public static void LoadAirports(string filePath)
        {
            using (StreamReader reader = new StreamReader(filePath))
            {
                JObject destinationsJsonFile = JObject.Parse(reader.ReadToEnd());
                Airports = destinationsJsonFile["data"]["destinationList"].Children<JObject>().ToDictionary(k => ((Airport)k.ToObject<Airport>()).Code, v => (Airport)v.ToObject<Airport>());
            }
        }

        public static int GetAircraftCount()
        {
            return Aircrafts.Count;
        }

        public static Aircraft FindAircraft(string aircraftName)
        {
            foreach (KeyValuePair<string, Aircraft> keyAircraftPair in Aircrafts)
            {
                if (keyAircraftPair.Key.Contains(aircraftName))
                {
                    return keyAircraftPair.Value;
                }
            }

            return null;
        }

        // Load airports for select list items in flight search form.
        public static List<SelectListItem> GetAirportList(bool isOrigin)
        {
            List<SelectListItem> result = new List<SelectListItem>();
            foreach (KeyValuePair<string, Airport> keyAirportPair in Airports)
            {
                if (isOrigin && Airports[keyAirportPair.Key].SiaSiteOrigin)
                {
                    result.Add(new SelectListItem(keyAirportPair.Value.CityName + ", " + keyAirportPair.Value.CountryName + " (" + keyAirportPair.Value.Name + ")", keyAirportPair.Key));
                }
                if (!isOrigin && Airports[keyAirportPair.Key].SiaSiteDestination)
                {
                    result.Add(new SelectListItem(keyAirportPair.Value.CityName + ", " + keyAirportPair.Value.CountryName + " (" + keyAirportPair.Value.Name + ")", keyAirportPair.Key));
                }
            }

            return result;
        }
    }
}
