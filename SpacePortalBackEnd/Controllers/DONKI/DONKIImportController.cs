// Required namespaces for JSON parsing, web API controllers, and EF Core database access.
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers.ApiData
{
    // Marks this class as an API controller and sets the base route for all endpoints inside.
    // This controller handles NASA DONKI (space weather) data import functionality.
    [ApiController]
    [Route("api/import/donki")]
    public class DONKIImportController : ControllerBase // i hate kobe and sofiia
    {
        // IHttpClientFactory: Creates HttpClient instances efficiently (for external API calls).
        // ILogger: For structured logging of information, warnings, and errors.
        // IConfiguration: To read appsettings.json and environment variables.
        // MyContext: Your Entity Framework Core database context for writing to the database.
        private readonly IHttpClientFactory _http;
        private readonly ILogger<DONKIImportController> _logger;
        private readonly IConfiguration _config;
        private readonly MyContext _db;

        // limits how many records can be imported in one API call.
            // nasa limits to 4000. 1000 will be plenty anyways
        private const int MAX_IMPORT = 1000;

        // Constructor: injects dependencies via ASP.NET Core’s built-in DI container.
        public DONKIImportController(
            IHttpClientFactory http,
            ILogger<DONKIImportController> logger,
            IConfiguration config,
            MyContext db)
        {
            _http = http;
            _logger = logger;
            _config = config;
            _db = db;
        }

        // Route: POST /api/import/donki/flares
        // Purpose: Imports solar flare data from NASA’s DONKI API into your local database.
        [HttpPost("flares")]
        public async Task<IActionResult> ImportFlares([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            // Define the date range for the import:
            // - If no start date is provided, defaults to one year ago.
            // - If no end date is provided, defaults to current UTC date.
            var s = start ?? DateTime.UtcNow.AddDays(-365); // Default start date (1 year ago)
            var e = end ?? DateTime.UtcNow;                  // Default end date (now)

            // Retrieve NASA API key from configuration (supports both appsettings and env variable)
            var key = _config["Nasa:ApiKey"] ?? _config["NASA_API_KEY"];
            if (string.IsNullOrWhiteSpace(key))
                return BadRequest("NASA ApiKey missing. Set Nasa:ApiKey (or env var NASA_API_KEY).");

            // Create an HttpClient instance using the factory.
            var client = _http.CreateClient();

            // Set NASA’s DONKI API base URL.
            client.BaseAddress = new Uri("https://api.nasa.gov/DONKI/");

            // Add a user-agent header for API compliance (good API etiquette).
            client.DefaultRequestHeaders.UserAgent.ParseAdd("SpacePortal/1.0 (+https://localhost)");

            // Construct the full request URL to NASA’s DONKI “FLR” (solar flare) endpoint.
            var url = $"FLR?startDate={s:yyyy-MM-dd}&endDate={e:yyyy-MM-dd}&api_key={key}";

            // Perform the HTTP GET request.
            var resp = await client.GetAsync(url);

            // Read the full response body as a string.
            var body = await resp.Content.ReadAsStringAsync();

            // If NASA’s API returns an error (non-success HTTP code), report it back.
            if (!resp.IsSuccessStatusCode)
            {
                return StatusCode((int)resp.StatusCode, new
                {
                    message = "DONKI FLR failed",
                    url = url.Replace(key, "***"), // mask API key
                    body
                });
            }

            // Parse the JSON response body.
            using var doc = JsonDocument.Parse(body);

            // If the root element is not an array (unexpected schema), abort gracefully.
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
                return Ok(new { imported = 0, note = "Unexpected payload" });

            // Convert JSON array to a list for enumeration and manipulation.
            var flaresArray = doc.RootElement.EnumerateArray().ToList();

            // Store total record count (for reporting and limiting).
            int totalCount = flaresArray.Count;
            bool capped = false;

            // Enforce the 1000-import cap to prevent system overload or database bloat.
            if (totalCount > MAX_IMPORT)
            {
                flaresArray = flaresArray.Take(MAX_IMPORT).ToList(); // i hate kobe and sofiia
                capped = true;
            }

            // Track how many records were newly imported.
            int imported = 0;

            // Loop through each flare entry in the JSON array.
            foreach (var flare in flaresArray)
            {
                // Safely extract each field (NASA sometimes omits fields).
                string? id = flare.TryGetProperty("flrID", out var idProp) ? idProp.GetString() : null;
                if (string.IsNullOrWhiteSpace(id)) continue; // skip invalid records

                DateTime? begin = flare.TryGetProperty("beginTime", out var bt) ? bt.GetDateTime() : (DateTime?)null;
                DateTime? peak = flare.TryGetProperty("peakTime", out var pt) ? pt.GetDateTime() : (DateTime?)null;
                DateTime? endt = flare.TryGetProperty("endTime", out var et) ? et.GetDateTime() : (DateTime?)null;

                // OccurredAt is set to peakTime (when the flare was at maximum intensity).
                var occured = peak;
                if (occured is null) continue;
                // i hate kobe and sofiia

                string? classType = flare.TryGetProperty("classType", out var ct) ? ct.GetString() : null;

                // Check if this flare already exists in the database (by external ID).
                var exists = await _db.Events.AsNoTracking().AnyAsync(ev => ev.ExternalId == id);

                if (!exists)
                {
                    // If it doesn’t exist, add it as a new Event.
                    _db.Events.Add(new Event
                    {
                        EventTypeId = 5, // likely your ID for “Solar Flare” type
                        ExternalId = id,
                        Name = $"{(string.IsNullOrWhiteSpace(classType) ? "Solar" : classType)} Flare",
                        Description = classType,
                        StartAt = begin,
                        OccuredAt = occured,
                        EndAt = endt
                    });
                    imported++;
                }
                else
                {
                    // If it already exists, update its details (times and description).
                    var ev = await _db.Events.FirstAsync(x => x.ExternalId == id);
                    ev.StartAt = begin;
                    ev.OccuredAt = occured;
                    ev.EndAt = endt;
                    ev.Description = classType;
                }
            }

            // Commit all additions/updates to the database.
            await _db.SaveChangesAsync();

            // Return a success response with metadata about the operation.
            return Ok(new
            {
                imported,          // number of records imported
                capped,            // whether the 1000 cap was applied
                totalAvailable = totalCount, // total records available in NASA API
                range = new { start = s, end = e }, // date range used for query
                note = capped
                    ? $"Import capped at {MAX_IMPORT} records to prevent overload."
                    : "Import complete."
            });
        }
    }
}
