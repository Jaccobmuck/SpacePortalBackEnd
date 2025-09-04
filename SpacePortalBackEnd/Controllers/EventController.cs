using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class EventController : ControllerBase
    {
        private readonly MyContext _db;
        private readonly ILogger<Event> _logger;

        public EventController(MyContext db, ILogger<Event> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("[action]")]
        public async Task<IActionResult> getEvent()
        {
            try
            {
                var events = await _db.Events.ToListAsync();
                return Ok(events);
            }   
            catch (Exception e)
            {
                _logger.LogError(e, "error finding events");
                return StatusCode(500);
            }
        }
    }
}
