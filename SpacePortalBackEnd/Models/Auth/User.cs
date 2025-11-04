using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpacePortalBackEnd.Models.Auth
{
    [Table("User")]
    public class User
    {
        public long UserId { get; set; }

        public string? Email { get; set; }                 // optional (for now)
        [Required, MaxLength(64)]
        public string DisplayName { get; set; } = null!;   // required for login
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AboutMe { get; set; }
        public bool IsActive { get; set; } = true;         // required + default 1
        [Required]
        public string PasswordHash { get; set; } = null!;  // required
        public long RoleId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
