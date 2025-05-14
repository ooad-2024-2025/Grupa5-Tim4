using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NaPoso.Models;

namespace NaPoso.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Recenzija> Recenzija { get; set; }
        public DbSet<Oglas> Oglas { get; set; }
        public DbSet<Obavijest> Obavijest { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recenzija>().ToTable("Recenzija");
            modelBuilder.Entity<Oglas>().ToTable("Oglas");
            modelBuilder.Entity<Obavijest>().ToTable("Obavijest");

            base.OnModelCreating(modelBuilder);
        }
    }
}
