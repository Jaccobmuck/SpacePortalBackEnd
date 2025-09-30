using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models.Auth;

namespace SpacePortalBackEnd.Models
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options) : base(options) { }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<User> Users => Set<User>();
        public DbSet<Role> Roles => Set<Role>();
        public DbSet<UserRole> UserRoles => Set<UserRole>();

        protected override void OnModelCreating(ModelBuilder mb)
        {
            base.OnModelCreating(mb);

            // User
            mb.Entity<User>(e =>
            {
                e.ToTable("User");
                e.HasKey(u => u.UserId);
                e.HasIndex(u => u.Email).IsUnique();

                // database default for CreatedAt
                e.Property(u => u.CreatedAt)
                 .HasDefaultValueSql("GETUTCDATE()");

                e.Property(u => u.IsActive).HasDefaultValue(true);
            });

            // Role
            mb.Entity<Role>(e =>
            {
                e.ToTable("Role");
                e.HasKey(r => r.RoleId);
                e.Property(r => r.Name).IsRequired().HasMaxLength(50);
                e.HasIndex(r => r.Name).IsUnique();

                // Hardcode/seed roles
                e.HasData(
                    new Role { RoleId = 1, Name = "Guest", Description = "Read-only access" },
                    new Role { RoleId = 2, Name = "User", Description = "Standard user" },
                    new Role { RoleId = 3, Name = "Admin", Description = "Administrative" }
                );
            });

            // UserRole (composite key)
            mb.Entity<UserRole>(e =>
            {
                e.ToTable("UserRole");
                e.HasKey(ur => new { ur.UserId, ur.RoleId });

                e.HasOne(ur => ur.User)
                 .WithMany(u => u.UserRoles)
                 .HasForeignKey(ur => ur.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(ur => ur.Role)
                 .WithMany(r => r.UserRoles)
                 .HasForeignKey(ur => ur.RoleId)
                 .OnDelete(DeleteBehavior.Cascade);
            });
        }

    }
}
