using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Contracts;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Auth;
using SpacePortalBackEnd.Security;

namespace SpacePortalBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(MyContext db, IJwtTokenService jwt, ILogger<AuthController> log) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register([FromBody] RegisterRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        if (await db.Users.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "Email is already registered." });

        var (hash, salt) = PasswordHasher.HashPassword(req.Password);

        var user = new User
        {
            Email = email,
            DisplayName = req.DisplayName?.Trim(),
            PasswordHash = hash,
            PasswordSalt = salt,
            IsActive = true
            // CreatedAt is DB-generated via GETUTCDATE()
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        // assign default role "User"
        var roleId = await db.Roles.Where(r => r.Name == "User").Select(r => r.RoleId).SingleAsync();
        db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = roleId });
        await db.SaveChangesAsync();

        var roles = await db.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.Name)
            .ToArrayAsync();

        var (token, exp) = jwt.Create(user.Email, user.UserId, roles);
        return Ok(new AuthResponse(token, exp, user.Email, roles));
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || !user.IsActive)
            return Unauthorized(new { message = "Invalid email or password." });

        var ok = PasswordHasher.Verify(req.Password, user.PasswordSalt, user.PasswordHash);
        if (!ok) return Unauthorized(new { message = "Invalid email or password." });

        var roles = await db.UserRoles
            .Where(ur => ur.UserId == user.UserId)
            .Select(ur => ur.Role.Name)
            .ToArrayAsync();

        var (token, exp) = jwt.Create(user.Email, user.UserId, roles);
        return Ok(new AuthResponse(token, exp, user.Email, roles));
    }

    // Role assignment (Admin only). Avoids handing out Admin at register time.
    [Authorize(Roles = "Admin")]
    [HttpPost("assign-role")]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest req)
    {
        var email = req.Email.Trim().ToLowerInvariant();
        var roleName = req.RoleName.Trim();

        if (roleName is not ("Guest" or "User" or "Admin"))
            return BadRequest(new { message = "Invalid role." });

        var user = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null) return NotFound(new { message = "User not found." });

        var role = await db.Roles.SingleAsync(r => r.Name == roleName);

        var hasRole = await db.UserRoles.AnyAsync(ur => ur.UserId == user.UserId && ur.RoleId == role.RoleId);
        if (!hasRole)
        {
            db.UserRoles.Add(new UserRole { UserId = user.UserId, RoleId = role.RoleId });
            await db.SaveChangesAsync();
        }

        return Ok(new { message = $"Assigned role '{roleName}' to {email}." });
    }
}
