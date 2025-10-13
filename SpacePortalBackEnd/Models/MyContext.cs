using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models.Auth;

namespace SpacePortalBackEnd.Models
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options) : base(options) { }
        public DbSet<Event> Events { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var u = modelBuilder.Entity<User>();

            u.Property(x => x.DisplayName).IsRequired().HasMaxLength(64);
            u.HasIndex(x => x.DisplayName).IsUnique();

            u.Property(x => x.PasswordHash).IsRequired();
        }

    }
}
