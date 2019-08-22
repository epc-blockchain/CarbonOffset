using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CarbonOffset.Models;
using CarbonOffset.Services;

namespace CarbonOffset
{
    public class Startup
    {
        // Uri for SIA APIs
        private readonly string _siaApiUri = "https://apigw.singaporeair.com/api/";
        // API Key for SIA Destination API
        private readonly string _siaDestinationApiKey = "b7342v4eu7433yxysmbrzmv4whs8u4fh7yeymf9w9pb4ap45";
        // Secret to SHA256 hash for SIA Destination API Header
        private readonly string _siaDestinationSecret = "XDszXSaQNK";
        // API Key for SIA Flight Search API
        private readonly string _siaFlightSearchApiKey = "gbha9g4hjne4w93mvz5ufqw5";

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<CookiePolicyOptions>(options =>
            {
                // This lambda determines whether user consent for non-essential cookies is needed for a given request.
                options.CheckConsentNeeded = context => true;
                options.MinimumSameSitePolicy = SameSiteMode.None;
            });

            services.Configure<CarbonOffsetDatabaseSettings>(Configuration.GetSection(nameof(CarbonOffsetDatabaseSettings)));
            services.AddSingleton<ICarbonOffsetDatabaseSettings>(s => s.GetRequiredService<IOptions<CarbonOffsetDatabaseSettings>>().Value);
            services.AddSingleton<CarbonFlightOffsetService>();

            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);

            // Enable Http Client Factory by Names
            // Http Client for ICAO Carbon Emission API
            services.AddHttpClient("icaoCarbonEmissions", h =>
            {
                h.BaseAddress = new Uri("https://v4p4sz5ijk.execute-api.us-east-1.amazonaws.com/anbdata/environment/carbonemissions");
            });

            // Http Client for SIA Destination API
            services.AddHttpClient("siaDestinations", h =>
            {
                h.BaseAddress = new Uri(_siaApiUri + "uat/v1/");
                h.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                h.DefaultRequestHeaders.Add("x-csl-client-uuid", Guid.NewGuid().ToString());
                h.DefaultRequestHeaders.Add("api_key", _siaDestinationApiKey);
                using (SHA256 sha256 = SHA256.Create())
                {
                    // Current unix timestamp
                    int seconds = (int)(DateTime.Now.ToUniversalTime() - new DateTime(1970, 1, 1)).TotalSeconds;
                    // Generate hash from API key, secret and timestamp
                    byte[] hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(_siaDestinationApiKey + _siaDestinationSecret + seconds));
                    string sigHash = GetStringFromHash(hash);
                    h.DefaultRequestHeaders.Add("x-signature", sigHash);
                }
            });

            // Http Client for SIA Flight Search API
            services.AddHttpClient("siaFlightSearch", h =>
            {
                h.BaseAddress = new Uri(_siaApiUri);
                h.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                h.DefaultRequestHeaders.Add("apikey", _siaFlightSearchApiKey);
            });

            // Http Client for Currency Exchange Rate API
            services.AddHttpClient("exchangeRate", h =>
            {
                h.BaseAddress = new Uri("https://api.exchangeratesapi.io/latest");
            });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseCookiePolicy();

            app.UseMvc();
        }

        // Convert byte[] of hash value to string
        private static string GetStringFromHash(byte[] hash)
        {
            StringBuilder result = new StringBuilder();
            for (int i = 0; i < hash.Length; i++)
            {
                result.Append(hash[i].ToString("X2"));
            }
            return result.ToString();
        }
    }
}
