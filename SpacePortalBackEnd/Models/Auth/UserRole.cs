using System.ComponentModel.DataAnnotations.Schema;

namespace SpacePortalBackEnd.Models.Auth
{
    [Table("UserRole")]
    public class UserRole
    {
        public long UserId { get; set; }
        public User User { get; set; } = default!;
        public long RoleId { get; set; }
        public Role Role { get; set; } = default!;
    }
}
