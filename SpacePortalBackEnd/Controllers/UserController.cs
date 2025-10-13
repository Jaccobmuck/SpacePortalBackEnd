using Microsoft.AspNetCore.Mvc;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Auth;

namespace SpacePortalBackEnd.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly MyContext _context;
        public UserController(MyContext context, ILogger<UserController> logger)
        {
            _logger = logger;
            _context = context;
        }

        [HttpGet("[action]")]
        public IActionResult GetUsers()
        {
            try
            {
                var users = _context.User.ToList();
                return Ok(users);
            }
            catch (Exception e)
            {
                _logger.LogError("Error message: " + e.Message);
                _logger.LogError("Stack trace: " + e.StackTrace);
                return BadRequest();
            }
        }

        [HttpGet("[action]")]
        public IActionResult GetUserById(long id)
        {
            try
            {
                var item = _context.User.Find(id); // finds user based on id

                if (item == null)
                {
                    _logger.LogWarning($"Id not found: {id}");
                    return NotFound(); // 404
                }

                return Ok(item);
            }
            catch (Exception e)
            {
                _logger.LogError("Error message: " + e.Message);
                _logger.LogError("Stack trace: " + e.StackTrace);
                return BadRequest();
            }
        }

        [HttpPost("[action]")]
        public IActionResult CreateUser([FromBody] User user)
        {
            try
            {
                if (user == null)
                    return BadRequest("Invalid user data");

                var existingUser = _context.User.FirstOrDefault(u => u.DisplayName == user.DisplayName); // finds the user by username

                if (existingUser != null)
                    return Conflict("User already exists");

                // else
                _context.User.Add(user);
                _context.SaveChanges(); // save changes 

                return CreatedAtAction(nameof(GetUsers), new { id = user.UserId }, user);
            }
            catch (Exception e)
            {
                _logger.LogError("error: " + e.Message);
                return BadRequest("Failed to create user");
            }
        }

        [HttpPut("[action]")]
        public IActionResult UpdateUser(long id, [FromBody] User model)
        {
            try
            {
                if (id != model.UserId) // if the id doesn't match the user id -> return 400
                {
                    _logger.LogWarning("Id mismatch");
                    return BadRequest();
                }

                var user = _context.User.Find(id); // find the item async

                if (user == null) // if the item isn't found -> return 404
                {
                    _logger.LogWarning($"Id not found: {id}");
                    return NotFound();
                }

                user.DisplayName = model.DisplayName; // update the item
                user.PasswordHash = model.PasswordHash;
                user.Email = model.Email;

                _context.SaveChanges(); // save changes

                return NoContent(); // 204
            }
            catch (Exception e)
            {
                _logger.LogError("Error message: " + e.Message);
                _logger.LogError("Stack trace: " + e.StackTrace);
                return BadRequest();
            }
        }

        [HttpDelete("[action]")]
        public IActionResult DeleteUser(long id)
        {
            try
            {
                var user = _context.User.Find(id); // find the item async

                if (user == null) // if the item isn't found -> return 404
                {
                    _logger.LogWarning($"Id not found: {id}");
                    return NotFound();
                }

                // else
                _context.User.Remove(user);
                _context.SaveChanges(); // save changes

                return NoContent(); // 204
            }
            catch (Exception e)
            {
                _logger.LogError("Error message: " + e.Message);
                _logger.LogError("Stack trace: " + e.StackTrace);
                return BadRequest();
            }
        }
    }
}
