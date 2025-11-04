using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.DTOs;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Auth;

namespace SpacePortalBackEnd.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;
        private readonly MyContext _context;

        public UserController(MyContext context, ILogger<UserController> logger)
        {
            _logger = logger;
            _context = context;
        }

        // GET /api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserReadDto>>> GetUsers(CancellationToken ct)
        {
            var users = await _context.User
                .AsNoTracking()
                .Select(u => new UserReadDto
                {
                    UserId = u.UserId,
                    DisplayName = u.DisplayName,
                    Email = u.Email,
                    RoleId = u.RoleId
                })
                .ToListAsync(ct);

            return Ok(users);
        }

        // GET /api/users/123
        [HttpGet("{id:long}")]
        public async Task<ActionResult<UserReadDto>> GetUserById(long id, CancellationToken ct)
        {
            var u = await _context.User
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .Select(x => new UserReadDto
                {
                    UserId = x.UserId,
                    DisplayName = x.DisplayName,
                    Email = x.Email,
                    RoleId = x.RoleId
                })
                .FirstOrDefaultAsync(ct);

            if (u == null) return NotFound();
            return Ok(u);
        }

        // POST /api/users
        [HttpPost]
        public async Task<ActionResult<UserReadDto>> CreateUser([FromBody] UserCreateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            // Enforce uniqueness at DB level too (see model config below)
            var exists = await _context.User.AnyAsync(u =>
                u.DisplayName == dto.DisplayName || u.Email == dto.Email, ct);
            if (exists) return Conflict("A user with that display name or email already exists.");

            var user = new User
            {
                DisplayName = dto.DisplayName,
                Email = dto.Email,
                RoleId = dto.RoleId = 1 // default role, e.g., User
                // Password hashing should happen in an auth service; do NOT accept PasswordHash over the wire.
            };

            _context.User.Add(user);
            await _context.SaveChangesAsync(ct);

            var read = new UserReadDto
            {
                UserId = user.UserId,
                DisplayName = user.DisplayName,
                Email = user.Email,
                RoleId = user.RoleId
            };

            return CreatedAtAction(nameof(GetUserById), new { id = user.UserId }, read);
        }

        // PUT /api/users/123
        [HttpPut("{id:long}")]
        public async Task<IActionResult> UpdateUser(long id, [FromBody] UserUpdateDto dto, CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var user = await _context.User.FindAsync(new object?[] { id }, ct);
            if (user == null) return NotFound();

            // Optional: keep email immutable unless you also re-verify it
            user.DisplayName = dto.DisplayName ?? user.DisplayName;
            user.Email = dto.Email ?? user.Email;

            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        // DELETE /api/users/123
        [HttpDelete("{id:long}")]
        public async Task<IActionResult> DeleteUser(long id, CancellationToken ct)
        {
            var user = await _context.User.FindAsync(new object?[] { id }, ct);
            if (user == null) return NotFound();

            _context.User.Remove(user);
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        // PUT /api/users/123/role  (Admin only)
        [Authorize(Roles = "Admin")]
        [HttpPut("{id:long}/role")]
        public async Task<IActionResult> ChangeUserRole(long id, [FromBody] ChangeRoleDto dto, CancellationToken ct)
        {
            if (dto is null) return BadRequest("Role payload required.");

            var user = await _context.User.FindAsync(new object?[] { id }, ct);
            if (user == null) return NotFound();

            // (Optional but recommended) verify the role exists:
            var roleExists = await _context.Role.AnyAsync(r => r.RoleId == dto.RoleId, ct);
            if (!roleExists) return BadRequest("Role does not exist.");

            // (Optional) guardrails: prevent removing the last Admin, prevent self-demote, etc.

            user.RoleId = dto.RoleId;
            await _context.SaveChangesAsync(ct);
            return NoContent();
        }

        [HttpGet("{id:long}/profile")]
        public async Task<ActionResult<UserProfileDto>> GetProfile(long id)
        {
            var u = await _context.User
                .Where(x => x.UserId == id)
                .Select(x => new UserProfileDto
                {
                    DisplayName = x.DisplayName,
                    FirstName = x.FirstName,
                    LastName = x.LastName,
                    AboutMe = x.AboutMe,
                    Email = x.Email
                })
                .FirstOrDefaultAsync();

            if (u == null) return NotFound();
            return Ok(u);
        }
        [Authorize]
        [HttpPut("me")]
        public async Task<ActionResult<UserProfileDto>> UpdateMyAccount(
            [FromBody] UserProfileDto dto,
            CancellationToken ct)
        {
            if (!ModelState.IsValid) return ValidationProblem(ModelState);

            var idStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                        ?? User.FindFirst("UserId")?.Value;

            if (!long.TryParse(idStr, out var userId))
                return Unauthorized("Invalid or missing user ID in token.");

            var user = await _context.User.FindAsync(new object?[] { userId }, ct);
            if (user is null) return NotFound();

            // Uniqueness checks (only if changed)
            if (!string.IsNullOrWhiteSpace(dto.DisplayName) && dto.DisplayName != user.DisplayName)
            {
                var dupName = await _context.User.AnyAsync(u => u.DisplayName == dto.DisplayName && u.UserId != userId, ct);
                if (dupName) return Conflict("Display name already taken.");
                user.DisplayName = dto.DisplayName;
            }

            if (!string.IsNullOrWhiteSpace(dto.Email) && dto.Email != user.Email)
            {
                var dupEmail = await _context.User.AnyAsync(u => u.Email == dto.Email && u.UserId != userId, ct);
                if (dupEmail) return Conflict("Email already in use.");
                user.Email = dto.Email;
            }

            // Update other fields (only if provided)
            if (dto.FirstName is not null) user.FirstName = dto.FirstName;
            if (dto.LastName is not null) user.LastName = dto.LastName;
            if (dto.AboutMe is not null) user.AboutMe = dto.AboutMe;

            await _context.SaveChangesAsync(ct);

            // Return the updated profile
            var result = new UserProfileDto
            {
                DisplayName = user.DisplayName,
                FirstName = user.FirstName,
                LastName = user.LastName,
                AboutMe = user.AboutMe,
                Email = user.Email
            };

            return Ok(result);
        }
    }
}
