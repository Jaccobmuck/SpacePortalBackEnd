using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers.ApiData
{
    [ApiController]
    [Route("api/import/donki")]
    public class DONKIImportController : ControllerBase
    {
        // Factory for http client instances, ASP.Net Core built-in DI
        private readonly IHttpClientFactory _http; // Making http reqs
        private readonly ILogger<DONKIImportController> _logger; // logging errors and info
        private readonly IConfiguration _config; // Accessing config settings -> appsettings.json or env vars
        private readonly MyContext _db; // Database context for accessing data

        // Constructor injects http client factory, logger, config, and database context
        public DONKIImportController(IHttpClientFactory http, ILogger<DONKIImportController> logger, IConfiguration config, MyContext db)
        {
            _http = http;
            _logger = logger;
            _config = config;
            _db = db;
        }

        [HttpPost("flares")]
        // POST /api/import/donki/flares?start=2016-01-01&end=2016-01-30
        // Both query params are optional. If nothing is entered, defaults to the last 30 days.
        public async Task<IActionResult> ImportFlares([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            var s = start ?? DateTime.UtcNow.AddDays(-30);//   s = start (default: now-30d UTC) 
            var e = end ?? DateTime.UtcNow;//   e = end   (default: now UTC)
            // this is how the default to the last 30 days is used/accomplished. 

            // NASA API key. Preferred path is "Nasa:ApiKey" (appsettings/user-secrets).
            // The env var "NASA_API_KEY" is used as a fallback if the first is missing.
            var key = _config["Nasa:ApiKey"] ?? _config["NASA_API_KEY"];
            if (string.IsNullOrWhiteSpace(key))
            {
                // 400 = client error; caller must supply/configure a key that is valid
                return BadRequest("NASA ApiKey missing. Set Nasa:ApiKey (or env var NASA_API_KEY).");
            }

            // Create a typed HttpClient targeting DONKI with a friendly User-Agent.
            var client = _http.CreateClient();
            client.BaseAddress = new Uri("https://api.nasa.gov/DONKI/");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SpacePortal/1.0 (+https://localhost)");

            // Build the FLR (Solar Flare) endpoint URL for the specified date window.
            var url = $"FLR?startDate={s:yyyy-MM-dd}&endDate={e:yyyy-MM-dd}&api_key={key}"; // key is in appsettings.json

            // Issue the GET request and capture the raw body for parsing.
            var resp = await client.GetAsync(url);
            var body = await resp.Content.ReadAsStringAsync();

            // If NASA rejected the call (e.g., invalid/missing key or rate limit),
            if (!resp.IsSuccessStatusCode)
            {   
                // something went wrong with the request
                return StatusCode((int)resp.StatusCode, new 
                { 
                    message = "DONKI FLR failed",
                    // Mask the key so it doesn't leak into logs or responses
                    url = url.Replace(key, "***"), 
                    body 
                });
            }

            // Parse the JSON body and ensure it's an array of flares.
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                // NASA changed shape or returned something unexpected; fail soft with a note.
                return Ok(new { imported = 0, note = "Unexpected payload" });
            }

            // Count how many new rows we actually inserted (not including updates)
            // not neccessarily needed but is nice to have
            int imported = 0;

            // Loop over each flare in the array.
            foreach (var flare in doc.RootElement.EnumerateArray())
            {
                // Unique identifier (used to dedupe imports across runs). Field: "flrID"
                string? id = flare.TryGetProperty("flrID", out var idProp) ? idProp.GetString() : null;

                // Skip any records without an ID—can't safely dedupe or reference them
                if (string.IsNullOrWhiteSpace(id)) continue;

                // Extract the begin, peak, and end times (if present)
                DateTime? begin = flare.TryGetProperty("beginTime", out var bt) ? bt.GetDateTime() : (DateTime?)null;
                DateTime? peak = flare.TryGetProperty("peakTime", out var pt) ? pt.GetDateTime() : (DateTime?)null;
                DateTime? endt = flare.TryGetProperty("endTime", out var et) ? et.GetDateTime() : (DateTime?)null;

                var occured = peak; // peak is the actual time it "occured" 
                if (occured is null) continue;

                string? classType = flare.TryGetProperty("classType", out var ct) ? ct.GetString() : null;

                // Idempotency check: does an Event with this ExternalId already exist?
                // AsNoTracking for performance since we only need existence.
                var exists = await _db.Events.AsNoTracking().AnyAsync(ev => ev.ExternalId == id);
                if (!exists)
                {
                    // Insert a new Event row populated from the DONKI payload.
                    _db.Events.Add(new Event
                    {
                        EventTypeId = 5, // SolarFlare. hard coded to be id = 5
                        ExternalId = id,
                        Name = $"{(string.IsNullOrWhiteSpace(classType) ? "Solar" : classType)} Flare",
                        Description = classType,
                        StartAt = begin,    
                        OccuredAt = occured,  
                        EndAt = endt
                    });
                    imported++;// track inserts (not updates)
                }
                else
                {
                    // Optional upsert behavior if you re-run imports:
                    // This can happen if NASA later refines times or adds more details.
                    var ev = await _db.Events.FirstAsync(x => x.ExternalId == id);
                    ev.StartAt = begin;
                    ev.OccuredAt = occured;
                    ev.EndAt = endt;
                    ev.Description = classType;
                }
            }

            await _db.SaveChangesAsync();
            return Ok(new 
            {
                // Return effective date range used for the fetch.
                imported, 
                range = new 
                { 
                    start = s, end = e 
                } 
            });
        }
    }
}
