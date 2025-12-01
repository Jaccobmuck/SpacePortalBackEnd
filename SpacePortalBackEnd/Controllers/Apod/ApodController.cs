using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.DTOs.Nasa;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers.Apod
{
    [ApiController]
    [Route("api/apod")]
    public class ApodController : ControllerBase
    {
        private readonly MyContext _db;

        public ApodController(MyContext db)
        {
            _db = db;
        }

        // GET /api/apod/today
        [HttpGet("today")]
        public async Task<IActionResult> GetToday(CancellationToken ct)
        {
            var today = DateTime.UtcNow.Date;

            var entity = await _db.ApodEntry
                .Where(a => a.Date <= today)
                .OrderByDescending(a => a.Date)
                .FirstOrDefaultAsync(ct);

            if (entity == null)
                return NotFound();

            return Ok(new ApodDto(entity));
        }

        // GET /api/apod/{date}
        // date: yyyy-MM-dd
        [HttpGet("{date}")]
        public async Task<IActionResult> GetByDate(DateTime date, CancellationToken ct)
        {
            var day = date.Date;

            var entity = await _db.ApodEntry
                .FirstOrDefaultAsync(a => a.Date == day, ct);

            if (entity == null)
                return NotFound();

            return Ok(new ApodDto(entity));
        }

        // GET /api/apod/recent?limit=30
        [HttpGet("recent")]
        public async Task<IActionResult> GetRecent(
            [FromQuery] int limit = 30,
            CancellationToken ct = default)
        {
            limit = Math.Clamp(limit, 1, 100);

            var items = await _db.ApodEntry
                .OrderByDescending(a => a.Date)
                .Take(limit)
                .ToListAsync(ct);

            var dtos = items.Select(e => new ApodDto(e)).ToList();

            return Ok(dtos);
        }
    }
}
