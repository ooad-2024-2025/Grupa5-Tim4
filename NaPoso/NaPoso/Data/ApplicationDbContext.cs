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
        public DbSet<PaymentTransaction> PaymentTransactions { get; set; }
        public DbSet<PrijavaRecenzije> PrijavaRecenzije { get; set; }

        /// <summary>
        /// Idempotent handler for Stripe webhook events — only processes each event once.
        /// </summary>
        public async Task HandleStripePaymentEventAsync(
            string paymentIntentId,
            string stripeEventId,
            PaymentStatus newStatus,
            long amount,
            string currency)
        {
            // Idempotency: skip if we already processed this event
            var alreadyProcessed = await PaymentTransactions
                .AnyAsync(p => p.StripeEventId == stripeEventId);
            if (alreadyProcessed)
                return;

            var transaction = await PaymentTransactions
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            if (transaction == null)
            {
                transaction = new PaymentTransaction
                {
                    StripePaymentIntentId = paymentIntentId,
                    StripeEventId = stripeEventId,
                    Status = newStatus,
                    Amount = amount,
                    Currency = currency,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PaidAt = newStatus == PaymentStatus.Paid ? DateTime.UtcNow : null
                };
                PaymentTransactions.Add(transaction);
            }
            else
            {
                transaction.Status = newStatus;
                transaction.Amount = amount;
                transaction.StripeEventId = stripeEventId;
                transaction.UpdatedAt = DateTime.UtcNow;
                if (newStatus == PaymentStatus.Paid)
                    transaction.PaidAt = DateTime.UtcNow;
            }

            await SaveChangesAsync();
        }

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
            modelBuilder.Entity<PaymentTransaction>().ToTable("PaymentTransaction");
            modelBuilder.Entity<OdobreniDokumenti>().ToTable("OdobreniDokumenti");
            modelBuilder.Entity<PrijavaRecenzije>().ToTable("PrijavaRecenzije");

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

            // PaymentTransaction indexes
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.StripePaymentIntentId)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.StripeEventId)
                .IsUnique();

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.UserId);

            // Globalni soft-delete query filter: primijenjuje se na SVE upite prema Oglas tabli (AsNoTracking, Find, Where, Include…)
            // Za obrise: automatski iskljucuje IsDeleted==true iz rezultata.
            // Admin ili drugi slucajevi koji trebaju vidjeti obrisane koriste: .IgnoreQueryFilters()
            modelBuilder.Entity<Oglas>()
                .HasQueryFilter(o => !o.IsDeleted);
        }
    }
}
