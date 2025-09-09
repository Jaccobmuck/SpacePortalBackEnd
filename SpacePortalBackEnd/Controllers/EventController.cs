using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
        // All roles will have access to this endpoint
        public async Task<IActionResult> getEvent() // all roles will have access. 
        {
            try
            {
                var events = await _db.Events.ToListAsync(); // List of events, waits for them to load before moving on
                return Ok(events); // Returns the events -> 200
            }   
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace, "error finding events"); // Message and Stack trace
                return StatusCode(500, "Internal Server Error"); // 500 error
            }
        }

        [HttpPost("[action]")]
        // Only admins will have access to this endpoint
        public async Task<IActionResult> AddEvent([FromBody] Event newEvent)
        {
            try
            {
                if(newEvent == null) // Can't add an event if it doesn't exist
                {
                    return BadRequest("Event is null"); // 400 
                }
                var model = new Event // Creates an event based on the event model
                {
                    EventTypeId = newEvent.EventTypeId,
                    ExternalId = newEvent.ExternalId,
                    Description = newEvent.Description,
                    Name = newEvent.Name
                };
                _db.Events.Add(model); // Add the model (event) to the db
                await _db.SaveChangesAsync(); // Wait for the db to save changes successfully before saying OK

                return Ok(model); // 200
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace, "Error adding event");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateEvent([FromBody] Event value, long id)
        {
            if (value == null)
            {
                return BadRequest("Event Data cannot be null");
            }
            try
            {
                var Event = await _db.Events.FindAsync(id);
                
                if(Event == null)
                {
                    return NotFound($"Event with id {id} does not exist");
                }

                Event.EventTypeId = value.EventTypeId;
                Event.ExternalId = value.ExternalId;
                Event.Description = value.Description;
                Event.Name = value.Name;

                await _db.SaveChangesAsync();
                
                return Ok(Event);
            }
            catch(Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace, "Error updating event");
                return StatusCode(500, "Internal Server Error");
            }
        }

        [HttpDelete("[action]")]
        public async Task<IActionResult> DeleteEvent(long id)
        {
            try
            {
                var Event = await _db.Events.FindAsync(id);
                if(Event == null)
                {
                    return NotFound($"Event with ID {id} not found");
                }

                _db.Remove(Event);
                await _db.SaveChangesAsync();
                return Ok("Event deleted successfully");
            }
            catch (Exception e)
            {
                _logger.LogError(e.Message, e.StackTrace, "Error updating event");
                return StatusCode(500, "Internal Server Error");
            }
        }
    }
}
