using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.DTOs.Nasa;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Nasa;

namespace SpacePortalBackEnd.Controllers.Apod
{
    [ApiController]
    [Route("api/import/nasa/apod")]
    public class ApodImportController : ControllerBase
    {
        private readonly IHttpClientFactory _http;
        private readonly ILogger<ApodImportController> _logger;
        private readonly IConfiguration _config;
        private readonly MyContext _db;
        private readonly IWebHostEnvironment _env;

        public ApodImportController(
            IHttpClientFactory http,
            ILogger<ApodImportController> logger,
            IConfiguration config,
            MyContext db,
            IWebHostEnvironment env)
        {
            _http = http;
            _logger = logger;
            _config = config;
            _db = db;
            _env = env;
        }

        // POST /api/import/nasa/apod?date=YYYY-MM-DD
        // If date is omitted, imports "today" (UTC date).
        [HttpPost]
        public async Task<IActionResult> ImportSingle(
            [FromQuery] DateTime? date,
            CancellationToken ct)
        
        {
            var day = (date ?? DateTime.UtcNow).Date;

            var key = _config["Nasa:ApiKey"];
            if (string.IsNullOrWhiteSpace(key))
            {
                return StatusCode(500, "Nasa:ApiKey is not configured.");
            }

            var client = _http.CreateClient("NasaApod");

            var url =
                $"planetary/apod?api_key={key}" +
                $"&date={day:yyyy-MM-dd}" +
                $"&thumbs=true";

            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("APOD single import failed: {Status} {Body}", resp.StatusCode, body);
                return StatusCode((int)resp.StatusCode, "NASA APOD request failed.");
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);

            var dto = await JsonSerializer.DeserializeAsync<ApodResponseDto>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct);

            if (dto == null)
            {
                return StatusCode(500, "NASA APOD returned no data.");
            }

            var entity = await UpsertApodAsync(dto, ct);

            return Ok(new ApodDto(entity));
        }

        // POST /api/import/nasa/apod/range?start=YYYY-MM-DD&end=YYYY-MM-DD
        [HttpPost("range")]
        public async Task<IActionResult> ImportRange(
            [FromQuery] DateTime start,
            [FromQuery] DateTime end,
            CancellationToken ct)
        {
            start = start.Date;
            end = end.Date;

            if (end < start)
            {
                return BadRequest("end must be on or after start.");
            }

            var key = _config["Nasa:ApiKey"];
            if (string.IsNullOrWhiteSpace(key))
            {
                return StatusCode(500, "Nasa:ApiKey is not configured.");
            }

            var client = _http.CreateClient("NasaApod");

            var url =
                $"planetary/apod?api_key={key}" +
                $"&start_date={start:yyyy-MM-dd}" +
                $"&end_date={end:yyyy-MM-dd}" +
                $"&thumbs=true";

            using var resp = await client.GetAsync(url, ct);
            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct);
                _logger.LogError("APOD range import failed: {Status} {Body}", resp.StatusCode, body);
                return StatusCode((int)resp.StatusCode, "NASA APOD range request failed.");
            }

            await using var stream = await resp.Content.ReadAsStreamAsync(ct);

            var dtos = await JsonSerializer.DeserializeAsync<List<ApodResponseDto>>(
                stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                ct) ?? new List<ApodResponseDto>();

            var results = new List<ApodDto>();

            foreach (var dto in dtos.OrderBy(d => d.date))
            {
                var entity = await UpsertApodAsync(dto, ct);
                results.Add(new ApodDto(entity));
            }

            return Ok(results);
        }

        // ------------- Internal helpers -------------

        private async Task<ApodEntry> UpsertApodAsync(
            ApodResponseDto dto,
            CancellationToken ct)
        {
            var date = DateTime.Parse(dto.date).Date;
            var now = DateTime.UtcNow;

            var entity = await _db.ApodEntry
                .FirstOrDefaultAsync(a => a.Date == date, ct);

            if (entity == null)
            {
                entity = new ApodEntry
                {
                    Date = date,
                    CreatedUtc = now
                };
                _db.ApodEntry.Add(entity);
            }

            entity.Title = dto.title;
            entity.Explanation = dto.explanation;
            entity.MediaType = dto.media_type;
            entity.Url = dto.url;
            entity.HdUrl = dto.hdurl;
            entity.Copyright = dto.copyright;

            if (dto.media_type.Equals("video", StringComparison.OrdinalIgnoreCase))
            {
                // For videos, NASA returns thumbnail_url when thumbs=true
                entity.ThumbUrl = dto.thumbnail_url;
            }
            else
            {
                // For images, convenient to use the main URL as thumbnail too
                entity.ThumbUrl = dto.url;

                // Optional: download image locally
                entity.LocalPath = await DownloadImageAsync(dto.hdurl ?? dto.url, date, ct);
            }

            entity.UpdatedUtc = now;

            await _db.SaveChangesAsync(ct);
            return entity;
        }

        private async Task<string?> DownloadImageAsync(
            string url,
            DateTime date,
            CancellationToken ct)
        {
            try
            {
                var client = _http.CreateClient("NasaApod");

                using var resp = await client.GetAsync(url, ct);
                resp.EnsureSuccessStatusCode();

                var bytes = await resp.Content.ReadAsByteArrayAsync(ct);

                var uri = new Uri(url);
                var ext = Path.GetExtension(uri.AbsolutePath);
                if (string.IsNullOrEmpty(ext))
                    ext = ".jpg";

                var relativeFolder = Path.Combine(
                    "apod",
                    date.Year.ToString("D4"),
                    date.Month.ToString("D2"));

                var fileName = $"{date:yyyyMMdd}{ext}";
                var relativePath = Path.Combine(relativeFolder, fileName);

                var root = _env.WebRootPath;
                var fullFolder = Path.Combine(root, relativeFolder);
                Directory.CreateDirectory(fullFolder);

                var fullPath = Path.Combine(fullFolder, fileName);
                await System.IO.File.WriteAllBytesAsync(fullPath, bytes, ct);

                // return as URL-style path: /apod/yyyy/mm/yyyymmdd.jpg
                return "/" + relativePath.Replace('\\', '/');
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to download APOD image from {Url}", url);
                return null;
            }
        }
    }
}
