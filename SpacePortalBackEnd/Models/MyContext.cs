using Microsoft.EntityFrameworkCore;
using SpacePortalBackEnd.Models.Auth;
using SpacePortalBackEnd.Models.Nasa; // <-- for ApodEntry

namespace SpacePortalBackEnd.Models
{
    public class MyContext : DbContext
    {
        public MyContext(DbContextOptions<MyContext> options) : base(options) { }

        public DbSet<Event> Events { get; set; }
        public DbSet<EventType> EventTypes { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<Role> Role { get; set; }

        // APOD entries
        public DbSet<ApodEntry> ApodEntry { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // ----- User config -----
            var u = modelBuilder.Entity<User>();

            u.Property(x => x.DisplayName)
                .IsRequired()
                .HasMaxLength(64);

            u.HasIndex(x => x.DisplayName)
                .IsUnique();

            u.Property(x => x.PasswordHash)
                .IsRequired();

            // ----- APOD config -----
            var apod = modelBuilder.Entity<ApodEntry>();

            apod.ToTable("ApodEntry");

            apod.HasIndex(x => x.Date)
                .IsUnique(); // one APOD row per calendar date

            apod.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(512);

            apod.Property(x => x.MediaType)
                .IsRequired()
                .HasMaxLength(50);

            apod.Property(x => x.Url)
                .IsRequired()
                .HasMaxLength(2048);

            apod.Property(x => x.HdUrl)
                .HasMaxLength(2048);

            apod.Property(x => x.ThumbUrl)
                .HasMaxLength(2048);

            apod.Property(x => x.LocalPath)
                .HasMaxLength(2048);
        }
    }
}
