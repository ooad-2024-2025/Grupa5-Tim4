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
        public DbSet<ObavijestKorisniku> ObavijestKorisniku { get; set; }
        public DbSet<OglasKorisnik> OglasKorisnik { get; set; }
        public DbSet<Chat> Chat { get; set; }
        public DbSet<Message> Message { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Recenzija>().ToTable("Recenzija");
            modelBuilder.Entity<Oglas>().ToTable("Oglas");
            modelBuilder.Entity<Obavijest>().ToTable("Obavijest");
            modelBuilder.Entity<ObavijestKorisniku>().ToTable("ObavijestKorisniku");
            modelBuilder.Entity<OglasKorisnik>().ToTable("OglasKorisnik");
            modelBuilder.Entity<Chat>().ToTable("Chat");
            modelBuilder.Entity<Message>().ToTable("Message");

            base.OnModelCreating(modelBuilder);
        }
    }
}
