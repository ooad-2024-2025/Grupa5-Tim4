using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NaPoso.Models;

namespace NaPoso.Data
{
    public class ApplicationDbContext : IdentityDbContext<Korisnik>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<Korisnik> Korisnik { get; set; }
        public DbSet<Recenzija> Recenzija { get; set; }
        public DbSet<Oglas> Oglas { get; set; }
        public DbSet<Obavijest> Obavijest { get; set; }
        public DbSet<ObavijestKorisniku> ObavijestKorisniku { get; set; }
        public DbSet<OglasKorisnik> OglasKorisnik { get; set; }
        public DbSet<OdobreniDokumenti> OdobreniDokumenti { get; set; }
        public DbSet<Chat> Chat { get; set; }
        public DbSet<Poruka> Poruka { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Korisnik>().ToTable("Korisnik");
            modelBuilder.Entity<Recenzija>().ToTable("Recenzija");
            modelBuilder.Entity<Oglas>().ToTable("Oglas");
            modelBuilder.Entity<Obavijest>().ToTable("Obavijest");
            modelBuilder.Entity<ObavijestKorisniku>().ToTable("ObavijestKorisniku");
            modelBuilder.Entity<OglasKorisnik>().ToTable("OglasKorisnik");
            modelBuilder.Entity<OdobreniDokumenti>().ToTable("OdobreniDokumenti");
            modelBuilder.Entity<Chat>().ToTable("Chat");
            modelBuilder.Entity<Poruka>().ToTable("Poruka");

            modelBuilder.Entity<Poruka>()
                .HasOne(p => p.Chat)
                .WithMany(c => c.Poruke)
                .HasForeignKey(p => p.ChatId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Poruka>()
                .HasOne(p => p.Posiljaoc)
                .WithMany()
                .HasForeignKey(p => p.PosiljaocId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Korisnik1)
                .WithMany()
                .HasForeignKey(c => c.Korisnik1Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Korisnik2)
                .WithMany()
                .HasForeignKey(c => c.Korisnik2Id)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Chat>()
                .HasOne(c => c.Oglas)
                .WithMany()
                .HasForeignKey(c => c.OglasId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
