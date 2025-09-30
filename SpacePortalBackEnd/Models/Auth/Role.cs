using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpacePortalBackEnd.Models.Auth
{
    [Table("Role")]
    public class Role
    {
        public long RoleId { get; set; }
        [Required, MaxLength(50)]
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
