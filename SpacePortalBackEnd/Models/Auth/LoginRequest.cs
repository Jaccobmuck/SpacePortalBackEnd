namespace SpacePortalBackEnd.Models.Auth
{
    public class LoginRequest
    {
        public string DisplayName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
