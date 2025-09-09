using Microsoft.EntityFrameworkCore;

namespace SpacePortalBackEnd.Models
{
    public class MyContext : DbContext
    {
       public MyContext(DbContextOptions<MyContext> options) : base(options) { }

       public DbSet<Event> Events { get; set; }

    }
}
