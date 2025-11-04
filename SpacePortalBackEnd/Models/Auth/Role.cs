using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SpacePortalBackEnd.Models.Auth
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public long RoleId { get; set; }
        public string Name { get; set; } = string.Empty;

       // public ICollection<UserRole>
    }
}
