using System.ComponentModel.DataAnnotations;

namespace SpacePortalBackEnd.Contracts;

public record RegisterRequest(
    [Required, EmailAddress] string Email,
    [Required, MinLength(8), MaxLength(128)] string Password,
    string? DisplayName
);

public record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password
);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAtUtc,
    string Email,
    string[] Roles
);

public record AssignRoleRequest(
    [Required, EmailAddress] string Email,
    [Required] string RoleName // "Guest" | "User" | "Admin"
);
