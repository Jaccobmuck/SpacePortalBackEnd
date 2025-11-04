namespace SpacePortalBackEnd.DTOs
{
    public sealed class UserReadDto
    {
        public long UserId { get; set; }
        public string? DisplayName { get; set; }
        public string? Email { get; set; }
        public long RoleId { get; set; }
    }
}
