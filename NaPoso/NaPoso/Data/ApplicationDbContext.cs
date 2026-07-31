using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using NaPoso.Models;
using ILogger = Microsoft.Extensions.Logging.ILogger<NaPoso.Data.ApplicationDbContext>;

namespace NaPoso.Data
{
    public class ApplicationDbContext : IdentityDbContext<Korisnik>
    {
        private readonly ILogger _logger;

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ILogger logger = null)
            : base(options)
        {
            _logger = logger;
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
        /// PRESERVES existing metadata (UserId, OglasId, WorkerUserId, TipAmountFeninga) if they
        /// were already set by a previous checkout.session.completed event.
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
                _logger?.LogInformation(
                    "[HandleStripePaymentEventAsync] Nova transakcija kreirana. PiId={PiId}, Amount={Amt}, " +
                    "Status={Status}, TipAmountFeninga={TipF}",
                    paymentIntentId, amount, newStatus, transaction.TipAmountFeninga);
            }
            else
            {
                // Preserve existing values that are LARGER (more likely to include bakšiš that
                // was set by checkout.session.completed metadata or Success page fallback).
                var oldAmount = transaction.Amount;
                var oldTip = transaction.TipAmountFeninga;

                if (amount > oldAmount)
                {
                    transaction.Amount = amount;
                    _logger?.LogInformation(
                        "[HandleStripePaymentEventAsync] Amount AŽURIRAN (novi je veći). PiId={PiId}, " +
                        "OldAmount={Old}, NewAmount={New}, DiffFeninga={Diff}",
                        paymentIntentId, oldAmount, amount, amount - oldAmount);
                }
                else if (amount < oldAmount)
                {
                    _logger?.LogWarning(
                        "[HandleStripePaymentEventAsync] NOVI Amount je MANJI od postojećeg — zadržavamo stari! " +
                        "PiId={PiId}, IncomingAmount={Inc}, ExistingAmount={Exist}, DiffFeninga={Diff}. " +
                        "(Ovo znači da bakšiš koji je bio sačuvan u prethodnom koraku bi inače izgubljen.)",
                        paymentIntentId, amount, oldAmount, oldAmount - amount);
                    // NE postavljamo transaction.Amount = amount — zadržavamo veći (postojeći) iznos!
                }
                else
                {
                    // Iznosi su jednaki — samo osiguramo da je postavljen (redundantno za debug)
                    transaction.Amount = amount;
                }

                transaction.Status = newStatus;
                transaction.StripeEventId = stripeEventId;
                transaction.UpdatedAt = DateTime.UtcNow;
                if (newStatus == PaymentStatus.Paid)
                    transaction.PaidAt = DateTime.UtcNow;

                _logger?.LogInformation(
                    "[HandleStripePaymentEventAsync] Postojeća transakcija ažurirana. PiId={PiId}, " +
                    "FinalAmount={Amt}, TipAmountFeninga={TipF}, Status={Status}",
                    paymentIntentId, transaction.Amount, transaction.TipAmountFeninga, newStatus);
                // NOTE: Preserve any metadata (UserId, OglasId, WorkerUserId, TipAmountFeninga)
                // that was already written by the checkout.session.completed handler.
            }

            await SaveChangesAsync();
        }

        /// <summary>
        /// Writes checkout session metadata (UserId, OglasId, RadnikId, TipAmountFeninga)
        /// onto the PaymentTransaction. Called from checkout.session.completed webhook.
        /// Idempotent: if a field already has a non-default value, it is NOT overwritten.
        /// </summary>
        public async Task<bool> ApplyCheckoutSessionMetadataAsync(
            string paymentIntentId,
            string? userId,
            int? oglasId,
            string? workerUserId,
            long tipAmountFeninga)
        {
            var transaction = await PaymentTransactions
                .FirstOrDefaultAsync(p => p.StripePaymentIntentId == paymentIntentId);

            bool created = false;
            if (transaction == null)
            {
                // checkout.session.completed arrived BEFORE any payment_intent event.
                // Create a placeholder row; subsequent payment_intent events will fill
                // in Amount, Status, Currency via HandleStripePaymentEventAsync.
                transaction = new PaymentTransaction
                {
                    StripePaymentIntentId = paymentIntentId,
                    StripeEventId = null,
                    Status = PaymentStatus.Pending,
                    Currency = "usd",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                PaymentTransactions.Add(transaction);
                created = true;
            }

            bool changed = created;

            if (string.IsNullOrEmpty(transaction.UserId) && !string.IsNullOrEmpty(userId))
            {
                transaction.UserId = userId;
                changed = true;
            }

            if (!transaction.OglasId.HasValue && oglasId.HasValue)
            {
                transaction.OglasId = oglasId.Value;
                changed = true;
            }

            if (string.IsNullOrEmpty(transaction.WorkerUserId) && !string.IsNullOrEmpty(workerUserId))
            {
                transaction.WorkerUserId = workerUserId;
                changed = true;
            }

            if (transaction.TipAmountFeninga == 0 && tipAmountFeninga > 0)
            {
                transaction.TipAmountFeninga = tipAmountFeninga;
                changed = true;
            }

            if (changed)
            {
                transaction.UpdatedAt = DateTime.UtcNow;
                await SaveChangesAsync();
            }

            return changed;
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
                .IsUnique()
                .HasFilter("\"StripeEventId\" IS NOT NULL");

            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.UserId);

            // Unique index on StripeSessionId (filtered: only non-null) for idempotent
            // transaction creation from Success page using Stripe session_id
            modelBuilder.Entity<PaymentTransaction>()
                .HasIndex(p => p.StripeSessionId)
                .IsUnique()
                .HasFilter("\"StripeSessionId\" IS NOT NULL");

            // Globalni soft-delete query filter: primijenjuje se na SVE upite prema Oglas tabli (AsNoTracking, Find, Where, Include…)
            // Za obrise: automatski iskljucuje IsDeleted==true iz rezultata.
            // Admin ili drugi slucajevi koji trebaju vidjeti obrisane koriste: .IgnoreQueryFilters()
            modelBuilder.Entity<Oglas>()
                .HasQueryFilter(o => !o.IsDeleted);
        }
    }
}
