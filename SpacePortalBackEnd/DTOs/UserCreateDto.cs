namespace SpacePortalBackEnd.DTOs
{
    public sealed class UserCreateDto
    {
        public string DisplayName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public long RoleId { get; set; } // optional; server can default
        // No PasswordHash here. If you roll your own auth, accept a plain password ONLY on an auth endpoint and hash server-side.
    }
}
