using Microsoft.EntityFrameworkCore;
using CommandGame.Models;

namespace CommandGame.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; } //links User model to users table in the database
        public DbSet<Level> Levels { get; set; }
    }
}