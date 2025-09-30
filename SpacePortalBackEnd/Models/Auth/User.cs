using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace SpacePortalBackEnd.Models.Auth
{
    [Table("User")]
    public class User
    {
        public long UserId { get; set; }
        public string Email { get; set; }
        public string? DisplayName { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        [Required] public byte[] PasswordHash { get; set; } = default;
        [Required] public byte[] PasswordSalt { get; set; } = default;
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
