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
using CarbonOffset.Models;

namespace CarbonOffset.Services
{
    public class IcaoCarbonEmissionApiService
    {
        private readonly HttpClient _client;

        public IcaoCarbonEmissionApiService(HttpClient client, IConfiguration configuration)
        {
            client.BaseAddress = new Uri(configuration.GetConnectionString("IcaoCarbonEmissionApi"));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            client.DefaultRequestHeaders.Add("x-api-key", configuration["IcaoCarbonEmissionApiSettings:Key"]);
            _client = client;
        }

        // Carbon emission calculator methodology from https://www.icao.int/environmental-protection/CarbonOffset/Documents/Methodology%20ICAO%20Carbon%20Calculator_v10-2017.pdf
        public List<FlightDetails> GetCarbonEmissions(FlightDetails flightDetails)
        {
            // Generate JSON input data for API call
            StringBuilder sb = new StringBuilder();
            using (JsonWriter writer = new JsonTextWriter(new StringWriter(sb)))
            {
                writer.Formatting = Formatting.Indented;
                writer.WriteStartObject();
                writer.WritePropertyName("from");
                writer.WriteValue(flightDetails.IcaoOriginAirportCode);
                writer.WritePropertyName("to");
                writer.WriteValue(flightDetails.IcaoDestinationAirportCode);
                writer.WritePropertyName("travelclass");
                writer.WriteValue(((char)flightDetails.ClassType).ToString());
                writer.WritePropertyName("indicator");
                writer.WriteValue((flightDetails.IsMetric) ? "m" : "s");
                writer.WriteEndObject();
                writer.Close();
            }

            HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, "")
            {
                Content = new StringContent(sb.ToString(), Encoding.UTF8, "application/json")
            };

            string carbonEmissionJsonResult = _client.GetStringAsync(request.RequestUri).Result;
            return JsonConvert.DeserializeObject<List<FlightDetails>>(carbonEmissionJsonResult);
        }
    }
}
