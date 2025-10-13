using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpacePortalBackEnd.Models;
using SpacePortalBackEnd.Models.Auth;

namespace SpacePortalBackEnd.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly MyContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public AuthController(IConfiguration configuration, MyContext context, IPasswordHasher<User> passwordHasher)
    {
        _configuration = configuration;
        _context = context;
        _passwordHasher = passwordHasher;
    }

    [HttpPost("login")]
    public IActionResult Login([FromBody] Models.Auth.LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || string.IsNullOrWhiteSpace(request.Password))
            return BadRequest("DisplayName and password are required.");

        var userLite = _context.User
            .AsNoTracking()
            .Where(u => u.DisplayName == request.DisplayName)
            .Select(u => new { u.UserId, u.DisplayName, u.PasswordHash, u.RoleId })
            .FirstOrDefault();

        if (userLite == null || string.IsNullOrEmpty(userLite.PasswordHash))
            return Unauthorized("Invalid credentials");

        var userForHash = new User
        {
            UserId = userLite.UserId,
            DisplayName = userLite.DisplayName,
            RoleId = userLite.RoleId,
            PasswordHash = userLite.PasswordHash
        };

        var verify = _passwordHasher.VerifyHashedPassword(userForHash, userLite.PasswordHash, request.Password);
        if (verify == PasswordVerificationResult.Failed)
            return Unauthorized("Invalid credentials");

        var roleName = _context.Role
            .AsNoTracking()
            .Where(r => r.RoleId == userLite.RoleId)
            .Select(r => r.Name)
            .FirstOrDefault() ?? "Guest";

        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.Name, userLite.DisplayName),
        new Claim(ClaimTypes.Role, roleName),
        new Claim("UserId", userLite.UserId.ToString())
    };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return Ok(new
        {
            token = new JwtSecurityTokenHandler().WriteToken(token),
            user = new { userLite.UserId, DisplayName = userLite.DisplayName, Role = roleName }
        });
    }


    [HttpPost("register")]
    public IActionResult Register([FromBody] Models.Auth.RegisterRequest model)
    {
        if (string.IsNullOrWhiteSpace(model.DisplayName) || string.IsNullOrWhiteSpace(model.Password))
            return BadRequest("DisplayName and password are required.");

        if (_context.User.Any(u => u.DisplayName == model.DisplayName))
            return BadRequest("DisplayName already exists");

        // defaults to user role without this code.
        //var role = _context.Role.FirstOrDefault(r => r.Name == model.Role);
        //if (role == null)
        //    return BadRequest("Invalid role");

        var user = new User
        {
            DisplayName = model.DisplayName,
            Email = "default Email",
            RoleId = 2,
            IsActive = true,                 
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, model.Password);

        _context.User.Add(user);
        _context.SaveChanges();

        return Ok("User registered successfully");
    }

}