using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models;

namespace SpacePortalBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class EventTypeController : ControllerBase
    {
        private readonly MyContext _db; // Database context for accessing data
        private readonly ILogger<EventType> _logger; // Logger for error and info logging

        // Constructor injects database context and logger
        public EventTypeController(MyContext db, ILogger<EventType> logger)
        {
            _db = db;
            _logger = logger;
        }

        [HttpGet("[action]")]
        // All roles will have access to this endpoint
        public async Task<IActionResult> GetEventTypes() // Retrieves all events
        {
            try
            {
                var events = await _db.EventTypes.ToListAsync(); // Fetches all events asynchronously
                return Ok(events); // Returns the list of events with HTTP 200
            }
            catch (Exception e)
            {
                // Logs error message and stack trace
                _logger.LogError(e.Message, e.StackTrace, "error finding events");
                return StatusCode(500, "Internal Server Error"); // Returns HTTP 500 on error
            }
        }

        [HttpPost("[action]")]
        // Only admins will have access to this endpoint
        public async Task<IActionResult> AddEventType([FromBody] EventType newEvent) // Adds a new event
        {
            try
            {
                if (newEvent == null) // Validates input
                {
                    return BadRequest("Event is null"); // Returns HTTP 400 if input is null
                }
                var model = new EventType // Creates a new event instance
                {
                    Description = newEvent.Description
                };
                _db.EventTypes.Add(model); // Adds the event to the database
                await _db.SaveChangesAsync(); // Saves changes asynchronously

                return Ok(model); // Returns the created event with HTTP 200
            }
            catch (Exception e)
            {
                // Logs error message and stack trace
                _logger.LogError(e.Message, e.StackTrace, "Error adding event");
                return StatusCode(500, "Internal Server Error"); // Returns HTTP 500 on error
            }
        }

        [HttpPut("[action]/{id}")]
        // Updates an existing event by ID
        public async Task<IActionResult> UpdateEventType([FromBody] EventType value, long id)
        {
            if (value == null) // Validates input
            {
                return BadRequest("Event Data cannot be null"); // Returns HTTP 400 if input is null
            }
            try
            {
                var Event = await _db.EventTypes.FindAsync(id); // Finds the event by ID

                if (Event == null) // Checks if event exists
                {
                    return NotFound($"Event with id {id} does not exist"); // Returns HTTP 404 if not found
                }

                // Updates event properties
                Event.Description = value.Description;

                await _db.SaveChangesAsync(); // Saves changes asynchronously

                return Ok(Event); // Returns the updated event with HTTP 200
            }
            catch (Exception e)
            {
                // Logs error message and stack trace
                _logger.LogError(e.Message, e.StackTrace, "Error updating event");
                return StatusCode(500, "Internal Server Error"); // Returns HTTP 500 on error
            }
        }

        [HttpDelete("[action]")]
        // Deletes an event by ID
        public async Task<IActionResult> DeleteEventType(long id)
        {
            try
            {
                var Event = await _db.EventTypes.FindAsync(id); // Finds the event by ID
                if (Event == null) // Checks if event exists
                {
                    return NotFound($"Event with ID {id} not found"); // Returns HTTP 404 if not found
                }

                _db.Remove(Event); // Removes the event from the database
                await _db.SaveChangesAsync(); // Saves changes asynchronously
                return Ok("Event deleted successfully"); // Returns success message with HTTP 200
            }
            catch (Exception e)
            {
                // Logs error message and stack trace
                _logger.LogError(e.Message, e.StackTrace, "Error updating event");
                return StatusCode(500, "Internal Server Error"); // Returns HTTP 500 on error
            }
        }
    }
}