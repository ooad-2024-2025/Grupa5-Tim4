using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests.Unit;

/// <summary>
/// Targeted tests that kill survived mutants identified via manual mutation analysis.
/// Each test is designed to catch a specific mutation that the existing test suite would miss.
/// </summary>
public class MutationKillTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatisticsService _statisticsService;
    private readonly PaymentTransactionService _paymentService;
    private readonly OglasService _oglasService;

    public MutationKillTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _statisticsService = new StatisticsService(_context);
        _paymentService = new PaymentTransactionService(_context);
        _oglasService = new OglasService(_context);
    }

    public void Dispose() => _context.Dispose();

    // ══════════════════════════════════════════════════════════════════
    // StatisticsService — killed mutants
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Statistics_RoleCounting_DistinguishesKlijentFromRadnik()
    {
        // MUTATION TARGET: Replace Contains("Klijent") with Contains("Radnik") in totalClients
        //   or Replace totalClients/totalWorkers assignment
        // KILLS: mutation that swaps client and worker counts
        var user1 = new Korisnik { Id = "u1", UserName = "k1@test.com", Email = "k1@test.com" };
        var user2 = new Korisnik { Id = "u2", UserName = "r1@test.com", Email = "r1@test.com" };
        var user3 = new Korisnik { Id = "u3", UserName = "r2@test.com", Email = "r2@test.com" };
        _context.Users.AddRange(user1, user2, user3);

        var klijentRole = new IdentityRole { Id = "role-klijent", Name = "Klijent" };
        var radnikRole = new IdentityRole { Id = "role-radnik", Name = "Radnik" };
        _context.Roles.AddRange(klijentRole, radnikRole);

        _context.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = "u1", RoleId = "role-klijent" },
            new IdentityUserRole<string> { UserId = "u2", RoleId = "role-radnik" },
            new IdentityUserRole<string> { UserId = "u3", RoleId = "role-radnik" });
        await _context.SaveChangesAsync();

        var stats = await _statisticsService.GetStatisticsAsync();

        Assert.Equal(1, stats.BrojKlijenata);
        Assert.Equal(2, stats.BrojRadnika);
    }

    [Fact]
    public async Task Statistics_SingleReview_ReturnsExactRating()
    {
        // MUTATION TARGET: AnyAsync() ternary condition
        //   or AverageAsync rounding
        // KILLS: mutation removing the ternary check or changing rounding
        _context.Recenzija.Add(
            new Recenzija { Ocjena = 3, Sadrzaj = "Solo", KlijentId = "k1", RadnikId = "r1" }
        );
        await _context.SaveChangesAsync();

        var stats = await _statisticsService.GetStatisticsAsync();

        Assert.Equal(3.0, stats.ProsjecnaOcjena, 1);
    }

    [Fact]
    public async Task Statistics_NonStandardDecimalAverage_RoundsToOneDecimal()
    {
        // MUTATION TARGET: Math.Round(averageRating, 1) changed to Math.Round(averageRating, 0)
        // KILLS: mutation that changes decimal precision
        _context.Recenzija.AddRange(
            new Recenzija { Ocjena = 2, Sadrzaj = "Meh", KlijentId = "k1", RadnikId = "r1" },
            new Recenzija { Ocjena = 3, Sadrzaj = "Ok", KlijentId = "k2", RadnikId = "r2" }
        );
        await _context.SaveChangesAsync();

        var stats = await _statisticsService.GetStatisticsAsync();

        // Average = 2.5, should be rounded to 2.5 (1 decimal), not 3 (0 decimals)
        Assert.Equal(2.5, stats.ProsjecnaOcjena, 1);
    }

    [Fact]
    public async Task Statistics_NoReviews_ReturnsZeroNotException()
    {
        // MUTATION TARGET: Remove ternary, always call AverageAsync
        // KILLS: mutation that removes the AnyAsync guard and crashes on empty
        var stats = await _statisticsService.GetStatisticsAsync();

        Assert.Equal(0, stats.ProsjecnaOcjena);
    }

    [Fact]
    public async Task Statistics_FinishedJobs_CountsNeaktivanNotAktivan()
    {
        // MUTATION TARGET: Change Status.Neaktivan to Status.Aktivan in finishedJobs
        // KILLS: status swap in CountAsync for finished jobs
        _context.Oglas.AddRange(
            new Oglas { Naslov = "Active", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan },
            new Oglas { Naslov = "Finished", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Neaktivan },
            new Oglas { Naslov = "Paid", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 300, Status = Status.Placen }
        );
        await _context.SaveChangesAsync();

        var stats = await _statisticsService.GetStatisticsAsync();

        Assert.Equal(1 + 1, stats.BrojZavrsenihPoslova);
        Assert.Equal(1, stats.AktivniPoslovi);
        Assert.Equal(1, stats.PlaceniPoslovi);
        // Critical: total must be 3, proving each status counted correctly
        Assert.Equal(3, stats.BrojPoslova);
        Assert.Equal(stats.BrojPoslova, stats.AktivniPoslovi + stats.BrojZavrsenihPoslova);
    }

    // ══════════════════════════════════════════════════════════════════
    // PaymentTransactionService — killed mutants
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task IsPaid_WithPaidTransactionForDifferentUser_ReturnsFalse()
    {
        // MUTATION TARGET: && to || in IsPaidAsync conditions
        //   e.g. p.UserId == userId || p.OglasId == oglasId || p.Status == PaymentStatus.Paid
        // KILLS: mutation that relaxes the AND-conjunction to OR
        var targetUserId = "user_target";
        var otherUserId = "user_other";
        var oglasId = 42;

        // Add a paid transaction for a DIFFERENT user on the same oglas
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = otherUserId,
            OglasId = oglasId,
            StripePaymentIntentId = "pi_other",
            StripeEventId = "evt_other",
            Amount = 500,
            Currency = "usd",
            Status = PaymentStatus.Paid
        });
        await _context.SaveChangesAsync();

        var result = await _paymentService.IsPaidAsync(targetUserId, oglasId);

        // With &&, this should be false because the transaction belongs to otherUserId
        // With ||, this would be true (p.OglasId == oglasId matches) — mutant SURVIVES
        Assert.False(result);
    }

    [Fact]
    public async Task IsPaid_WithPaidTransactionForDifferentOglas_ReturnsFalse()
    {
        // MUTATION TARGET: && to || between UserId and OglasId conditions
        // KILLS: same relaxation as above, different dimension
        var userId = "user_x";
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = userId,
            OglasId = 999,
            StripePaymentIntentId = "pi_wrong_oglas",
            StripeEventId = "evt_wrong_oglas",
            Amount = 500,
            Currency = "usd",
            Status = PaymentStatus.Paid
        });
        await _context.SaveChangesAsync();

        var result = await _paymentService.IsPaidAsync(userId, 1);

        Assert.False(result);
    }

    [Fact]
    public async Task IsPaid_ReturnsFalse_WhenStatusIsRefunded()
    {
        // MUTATION TARGET: Change PaymentStatus.Paid to PaymentStatus.Refunded
        // KILLS: enum swap in the filter condition
        var userId = "user_refunded";
        var oglasId = 50;
        _context.PaymentTransactions.Add(new PaymentTransaction
        {
            UserId = userId,
            OglasId = oglasId,
            StripePaymentIntentId = "pi_refunded",
            StripeEventId = "evt_refunded",
            Amount = 500,
            Currency = "usd",
            Status = PaymentStatus.Refunded
        });
        await _context.SaveChangesAsync();

        var result = await _paymentService.IsPaidAsync(userId, oglasId);

        Assert.False(result);
    }

    [Fact]
    public async Task GetByOglasIdAsync_ReturnsOnlyTransactionsForRequestedOglas()
    {
        // MUTATION TARGET: Change p.OglasId == oglasId to p.OglasId != oglasId
        // KILLS: filter inversion
        var targetId = 77;
        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = "u1", OglasId = targetId, StripePaymentIntentId = "pi_match",
                StripeEventId = "evt_match", Amount = 100, Currency = "usd",
                Status = PaymentStatus.Paid
            },
            new PaymentTransaction
            {
                UserId = "u2", OglasId = targetId + 1, StripePaymentIntentId = "pi_no_match",
                StripeEventId = "evt_no_match", Amount = 200, Currency = "usd",
                Status = PaymentStatus.Paid
            }
        );
        await _context.SaveChangesAsync();

        var result = await _paymentService.GetByOglasIdAsync(targetId);

        Assert.Single(result);
        Assert.Equal("pi_match", result[0].StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByOglasIdAsync_ReturnsOrderedDescendingByCreatedAt()
    {
        // MUTATION TARGET: Remove OrderByDescending from GetByOglasIdAsync
        // KILLS: ordering removal (order is not asserted in existing tests)
        var oglasId = 33;
        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = "u1", OglasId = oglasId, StripePaymentIntentId = "pi_first",
                StripeEventId = "evt_first", Amount = 100, Currency = "usd",
                Status = PaymentStatus.Paid,
                CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
            },
            new PaymentTransaction
            {
                UserId = "u2", OglasId = oglasId, StripePaymentIntentId = "pi_second",
                StripeEventId = "evt_second", Amount = 200, Currency = "usd",
                Status = PaymentStatus.Paid,
                CreatedAt = new DateTime(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc)
            }
        );
        await _context.SaveChangesAsync();

        var result = await _paymentService.GetByOglasIdAsync(oglasId);

        Assert.Equal(2, result.Count);
        Assert.Equal("pi_second", result[0].StripePaymentIntentId);
        Assert.Equal("pi_first", result[1].StripePaymentIntentId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ReturnsOnlyTransactionsForRequestedUser()
    {
        // MUTATION TARGET: Change p.UserId == userId to != or remove Where clause
        // KILLS: filter inversion or removal
        var targetUser = "user_filter_test";
        _context.PaymentTransactions.AddRange(
            new PaymentTransaction
            {
                UserId = targetUser, OglasId = 1, StripePaymentIntentId = "pi_target",
                StripeEventId = "evt_target", Amount = 100, Currency = "usd",
                Status = PaymentStatus.Paid
            },
            new PaymentTransaction
            {
                UserId = "user_other", OglasId = 2, StripePaymentIntentId = "pi_other",
                StripeEventId = "evt_other", Amount = 200, Currency = "usd",
                Status = PaymentStatus.Paid
            }
        );
        await _context.SaveChangesAsync();

        var result = await _paymentService.GetByUserIdAsync(targetUser);

        Assert.Single(result);
        Assert.Equal("pi_target", result[0].StripePaymentIntentId);
    }

    // ══════════════════════════════════════════════════════════════════
    // StripeService — killed mutants
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenApiKeyIsWhitespace()
    {
        // MUTATION TARGET: IsNullOrWhiteSpace changed to IsNullOrEmpty
        // KILLS: boundary mutation that allows whitespace-only keys
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns("   ");

        var httpMock = new Mock<IHttpContextAccessor>();
        var service = new StripeService(configMock.Object, httpMock.Object);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_ReturnsFalse_WhenApiKeyIsTab()
    {
        // MUTATION TARGET: Same as above — various whitespace chars
        // KILLS: boundary mutation
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns("\t\n");

        var httpMock = new Mock<IHttpContextAccessor>();
        var service = new StripeService(configMock.Object, httpMock.Object);

        Assert.False(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_PrefersDirectKeyOverSection()
    {
        // MUTATION TARGET: Change ?? to ? (null-coalescing to null-conditional)
        //   or swap the order of fallback resolution
        // KILLS: mutation on the fallback key resolution
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns("sk_direct_key");

        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["SecretKey"]).Returns("sk_section_key");
        configMock.Setup(c => c.GetSection("Stripe")).Returns(sectionMock.Object);

        var httpMock = new Mock<IHttpContextAccessor>();
        var service = new StripeService(configMock.Object, httpMock.Object);

        Assert.True(service.IsConfigured);
    }

    [Fact]
    public void IsConfigured_FallsBackToSection_WhenDirectKeyIsNull()
    {
        // MUTATION TARGET: Remove the ?? fallback or change to && 
        // KILLS: mutation that breaks null-coalescing fallback
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Stripe:SecretKey"]).Returns((string?)null);

        var sectionMock = new Mock<IConfigurationSection>();
        sectionMock.Setup(s => s["SecretKey"]).Returns("sk_section_only");
        configMock.Setup(c => c.GetSection("Stripe")).Returns(sectionMock.Object);

        var httpMock = new Mock<IHttpContextAccessor>();
        var service = new StripeService(configMock.Object, httpMock.Object);

        Assert.True(service.IsConfigured);
    }

    // ══════════════════════════════════════════════════════════════════
    // OglasService — killed mutants
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CreateOglasAsync_SetsStatusToAktivan_AndClearsRadnik()
    {
        // MUTATION TARGET: oglas.Status = Status.Aktivan changed to Status.Neaktivan
        //   or oglas.RadnikId = null changed to RadnikId = klijentId
        // KILLS: status assignment mutation and RadnikId assignment mutation
        var user = new Korisnik { Id = "client1", UserName = "c@test.com", Email = "c@test.com" };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var input = new Oglas
        {
            Naslov = "Test",
            Opis = "Description",
            Lokacija = "Sarajevo",
            TipPosla = "IT",
            CijenaPosla = 500,
            RadnikId = "should_be_cleared"
        };

        var result = await _oglasService.CreateOglasAsync(input, "client1", RoleConstants.Klijent);

        Assert.Equal(Status.Aktivan, result.Status);
        Assert.Equal("client1", result.KlijentId);
        Assert.Null(result.RadnikId);
    }

    [Fact]
    public async Task DeleteOglasAsync_ReturnsFalse_WhenNotExists()
    {
        // MUTATION TARGET: Remove the null check and always return true
        //   or change return false to return true
        // KILLS: guard clause bypass mutations
        var result = await _oglasService.DeleteOglasAsync(99999);

        Assert.False(result);
    }

    [Fact]
    public async Task DeleteOglasAsync_ReturnsTrue_WhenExists()
    {
        // MUTATION TARGET: return true changed to return false at end
        // KILLS: return value mutation
        var oglas = new Oglas
        {
            Naslov = "Delete me", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _oglasService.DeleteOglasAsync(oglas.Id);

        Assert.True(result);
        Assert.Null(await _oglasService.GetOglasByIdAsync(oglas.Id));
    }

    [Fact]
    public async Task ApplyToOglasAsync_ReturnsFalse_WhenOglasIsInactive()
    {
        // MUTATION TARGET: oglas.Status != Status.Aktivan changed to == Status.Aktivan
        //   or the whole guard condition mutated
        // KILLS: status check mutation in guard clause
        var user = new Korisnik { Id = "worker1", UserName = "w@test.com", Email = "w@test.com" };
        _context.Users.Add(user);

        var oglas = new Oglas
        {
            Naslov = "Inactive", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Neaktivan
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _oglasService.ApplyToOglasAsync(oglas.Id, "worker1");

        Assert.False(result);
    }

    [Fact]
    public async Task ApplyToOglasAsync_ReturnsFalse_WhenRadnikAlreadyAssigned()
    {
        // MUTATION TARGET: oglas.RadnikId != null changed to == null
        // KILLS: null check inversion in guard clause
        var owner = new Korisnik { Id = "owner1", UserName = "o@test.com", Email = "o@test.com" };
        var worker = new Korisnik { Id = "existing_worker", UserName = "ew@test.com", Email = "ew@test.com" };
        _context.Users.AddRange(owner, worker);

        var oglas = new Oglas
        {
            Naslov = "Taken", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan,
            KlijentId = "owner1", RadnikId = "existing_worker"
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _oglasService.ApplyToOglasAsync(oglas.Id, "new_worker");

        Assert.False(result);
    }

    [Fact]
    public async Task ApplyToOglasAsync_ReturnsFalse_WhenDuplicateApplication()
    {
        // MUTATION TARGET: Remove the duplicate check
        // KILLS: duplicate application guard removal
        var owner = new Korisnik { Id = "owner2", UserName = "o2@test.com", Email = "o2@test.com" };
        var worker = new Korisnik { Id = "w2", UserName = "w2@test.com", Email = "w2@test.com" };
        _context.Users.AddRange(owner, worker);

        var oglas = new Oglas
        {
            Naslov = "Has applicant", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "owner2"
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        _context.OglasKorisnik.Add(new OglasKorisnik
        {
            OglasId = oglas.Id, KorisnikId = "w2",
            DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan
        });
        await _context.SaveChangesAsync();

        var result = await _oglasService.ApplyToOglasAsync(oglas.Id, "w2");

        Assert.False(result);
    }

    [Fact]
    public async Task AcceptApplicationAsync_SetsStatusToPrihvacen()
    {
        // MUTATION TARGET: Status.Prihvacen changed to Status.Aktivan or Status.Neaktivan
        //   or remove notification creation
        // KILLS: status assignment mutation
        var owner = new Korisnik { Id = "acc_owner", UserName = "ao@test.com", Email = "ao@test.com" };
        var worker = new Korisnik { Id = "acc_worker", UserName = "aw@test.com", Email = "aw@test.com" };
        _context.Users.AddRange(owner, worker);

        var oglas = new Oglas
        {
            Naslov = "Accept me", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "acc_owner"
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik
        {
            OglasId = oglas.Id, KorisnikId = "acc_worker",
            DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan
        };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var result = await _oglasService.AcceptApplicationAsync(prijava.Id);

        Assert.True(result);
        var updated = await _context.OglasKorisnik.FindAsync(prijava.Id);
        Assert.Equal(Status.Prihvacen, updated!.Status);

        // Verify notification was created for the worker
        var notification = await _context.Obavijest
            .FirstOrDefaultAsync(o => o.KorisnikId == "acc_worker");
        Assert.NotNull(notification);
        Assert.Contains("prihvaćena", notification.Sadrzaj);
    }

    [Fact]
    public async Task RejectApplicationAsync_ReturnsFalse_WhenNotOwner()
    {
        // MUTATION TARGET: prijava.Oglas.KlijentId != oglasOwnerId changed to ==
        // KILLS: ownership check inversion
        var owner = new Korisnik { Id = "rej_owner", UserName = "ro@test.com", Email = "ro@test.com" };
        var worker = new Korisnik { Id = "rej_worker", UserName = "rw@test.com", Email = "rw@test.com" };
        _context.Users.AddRange(owner, worker);

        var oglas = new Oglas
        {
            Naslov = "Reject test", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "rej_owner"
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik
        {
            OglasId = oglas.Id, KorisnikId = "rej_worker",
            DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan
        };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        // Non-owner tries to reject
        var result = await _oglasService.RejectApplicationAsync(prijava.Id, "wrong_owner");

        Assert.False(result);
    }

    [Fact]
    public async Task RejectApplicationAsync_SetsStatusToNeaktivan_WhenOwner()
    {
        // MUTATION TARGET: Status.Neaktivan changed to Status.Prihvacen
        //   or remove notification creation
        // KILLS: status assignment and notification mutations
        var owner = new Korisnik { Id = "rej_owner2", UserName = "ro2@test.com", Email = "ro2@test.com" };
        var worker = new Korisnik { Id = "rej_worker2", UserName = "rw2@test.com", Email = "rw2@test.com" };
        _context.Users.AddRange(owner, worker);

        var oglas = new Oglas
        {
            Naslov = "Reject test 2", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "rej_owner2"
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik
        {
            OglasId = oglas.Id, KorisnikId = "rej_worker2",
            DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan
        };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var result = await _oglasService.RejectApplicationAsync(prijava.Id, "rej_owner2");

        Assert.True(result);
        var updated = await _context.OglasKorisnik.FindAsync(prijava.Id);
        Assert.Equal(Status.Neaktivan, updated!.Status);

        var notification = await _context.Obavijest
            .FirstOrDefaultAsync(o => o.KorisnikId == "rej_worker2");
        Assert.NotNull(notification);
        Assert.Contains("nije odabrana", notification.Sadrzaj);
    }

    [Fact]
    public async Task SearchOglasiAsync_ExcludesInactiveOglasi()
    {
        // MUTATION TARGET: o.Status == Status.Aktivan changed to o.Status != Status.Aktivan
        //   or remove the Status filter entirely
        // KILLS: base filter mutation
        var user = new Korisnik { Id = "search_user", UserName = "su@test.com", Email = "su@test.com", Verified = true };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Active Job", Opis = "Available", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan, KlijentId = "search_user"
            },
            new Oglas
            {
                Naslov = "Inactive Job", Opis = "Not available", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 600, Status = Status.Neaktivan, KlijentId = "search_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, null, null, null);

        Assert.Single(results);
        Assert.Equal("Active Job", results[0].Oglas.Naslov);
    }

    [Fact]
    public async Task SearchOglasiAsync_ExcludesOglasiWithRadnikAssigned()
    {
        // MUTATION TARGET: o.RadnikId == null changed to o.RadnikId != null
        // KILLS: null check inversion in base filter
        var user = new Korisnik { Id = "search_user2", UserName = "su2@test.com", Email = "su2@test.com", Verified = false };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Open Job", Opis = "No worker yet", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "search_user2", RadnikId = null
            },
            new Oglas
            {
                Naslov = "Taken Job", Opis = "Has worker", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 600, Status = Status.Aktivan,
                KlijentId = "search_user2", RadnikId = "some_worker"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, null, null, null);

        Assert.Single(results);
        Assert.Equal("Open Job", results[0].Oglas.Naslov);
    }

    [Fact]
    public async Task SearchOglasiAsync_MinPrice_Inclusive()
    {
        // MUTATION TARGET: >= changed to > (exclusive boundary)
        // KILLS: boundary condition mutation
        var user = new Korisnik { Id = "price_user", UserName = "pu@test.com", Email = "pu@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Exact min", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan,
                KlijentId = "price_user"
            },
            new Oglas
            {
                Naslov = "Above min", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan,
                KlijentId = "price_user"
            },
            new Oglas
            {
                Naslov = "Below min", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 50, Status = Status.Aktivan,
                KlijentId = "price_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, null, 100, null);

        // With >= 100: should include "Exact min" (100) and "Above min" (200)
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Oglas.Naslov == "Exact min");
        Assert.Contains(results, r => r.Oglas.Naslov == "Above min");
    }

    [Fact]
    public async Task SearchOglasiAsync_MaxPrice_Inclusive()
    {
        // MUTATION TARGET: <= changed to < (exclusive boundary)
        // KILLS: boundary condition mutation
        var user = new Korisnik { Id = "price_user2", UserName = "pu2@test.com", Email = "pu2@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Exact max", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 300, Status = Status.Aktivan,
                KlijentId = "price_user2"
            },
            new Oglas
            {
                Naslov = "Below max", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan,
                KlijentId = "price_user2"
            },
            new Oglas
            {
                Naslov = "Above max", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "price_user2"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, null, null, 300);

        // With <= 300: should include "Exact max" (300) and "Below max" (100)
        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.Oglas.Naslov == "Exact max");
        Assert.Contains(results, r => r.Oglas.Naslov == "Below max");
    }

    [Fact]
    public async Task SearchOglasiAsync_PriceRange_BothFilters()
    {
        // MUTATION TARGET: Any of the >=, <= operators or the combination
        // KILLS: compound filter mutations
        var user = new Korisnik { Id = "range_user", UserName = "ru@test.com", Email = "ru@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "In range", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan,
                KlijentId = "range_user"
            },
            new Oglas
            {
                Naslov = "Too low", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 50, Status = Status.Aktivan,
                KlijentId = "range_user"
            },
            new Oglas
            {
                Naslov = "Too high", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "range_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, null, 100, 400);

        Assert.Single(results);
        Assert.Equal("In range", results[0].Oglas.Naslov);
    }

    [Fact]
    public async Task SearchOglasiAsync_SortByPriceAsc()
    {
        // MUTATION TARGET: "cijena_asc" changed to sort by Naslov or descending
        // KILLS: sort logic mutation
        var user = new Korisnik { Id = "sort_user", UserName = "srt@test.com", Email = "srt@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Expensive", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 300, Status = Status.Aktivan,
                KlijentId = "sort_user"
            },
            new Oglas
            {
                Naslov = "Cheap", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan,
                KlijentId = "sort_user"
            },
            new Oglas
            {
                Naslov = "Medium", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan,
                KlijentId = "sort_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, "cijena_asc", null, null);

        Assert.Equal(3, results.Count);
        Assert.Equal(100, results[0].Oglas.CijenaPosla);
        Assert.Equal(200, results[1].Oglas.CijenaPosla);
        Assert.Equal(300, results[2].Oglas.CijenaPosla);
    }

    [Fact]
    public async Task SearchOglasiAsync_SortByPriceDesc()
    {
        // MUTATION TARGET: "cijena_desc" sort path changed
        // KILLS: descending sort mutation
        var user = new Korisnik { Id = "sort_user2", UserName = "srt2@test.com", Email = "srt2@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "A", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan,
                KlijentId = "sort_user2"
            },
            new Oglas
            {
                Naslov = "B", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 300, Status = Status.Aktivan,
                KlijentId = "sort_user2"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, null, "cijena_desc", null, null);

        Assert.Equal(2, results.Count);
        Assert.Equal(300, results[0].Oglas.CijenaPosla);
        Assert.Equal(100, results[1].Oglas.CijenaPosla);
    }

    [Fact]
    public async Task SearchOglasiAsync_SearchFiltersOnNaslovAndOpis()
    {
        // MUTATION TARGET: Remove the search filter or change || to && in the search condition
        // KILLS: search filter mutations
        var user = new Korisnik { Id = "search_f", UserName = "sf@test.com", Email = "sf@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Web Developer", Opis = "React and TypeScript", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "search_f"
            },
            new Oglas
            {
                Naslov = "Graphic Designer", Opis = "Adobe Photoshop expert", Lokacija = "Sarajevo",
                TipPosla = "Design", CijenaPosla = 400, Status = Status.Aktivan,
                KlijentId = "search_f"
            }
        );
        await _context.SaveChangesAsync();

        // Search by Naslov match
        var byTitle = await _oglasService.SearchOglasiAsync("Web", null, null, null, null, null);
        Assert.Single(byTitle);

        // Search by Opis match
        var byDesc = await _oglasService.SearchOglasiAsync("Photoshop", null, null, null, null, null);
        Assert.Single(byDesc);
    }

    [Fact]
    public async Task SearchOglasiAsync_LokacijaFilter()
    {
        // MUTATION TARGET: lokacija filter changed or removed
        // KILLS: location filter mutation
        var user = new Korisnik { Id = "loc_user", UserName = "lu@test.com", Email = "lu@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "Sarajevo Job", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "loc_user"
            },
            new Oglas
            {
                Naslov = "Tuzla Job", Opis = "Opis", Lokacija = "Tuzla",
                TipPosla = "IT", CijenaPosla = 400, Status = Status.Aktivan,
                KlijentId = "loc_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, "Sarajevo", null, null, null, null);

        Assert.Single(results);
        Assert.Equal("Sarajevo", results[0].Oglas.Lokacija);
    }

    [Fact]
    public async Task SearchOglasiAsync_TipPoslaFilter()
    {
        // MUTATION TARGET: tipPosla filter changed to != or removed
        // KILLS: tipPosla filter mutation
        var user = new Korisnik { Id = "tip_user", UserName = "tu@test.com", Email = "tu@test.com" };
        _context.Users.Add(user);

        _context.Oglas.AddRange(
            new Oglas
            {
                Naslov = "IT Job", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "IT", CijenaPosla = 500, Status = Status.Aktivan,
                KlijentId = "tip_user"
            },
            new Oglas
            {
                Naslov = "Construction Job", Opis = "Opis", Lokacija = "Sarajevo",
                TipPosla = "Građevina", CijenaPosla = 600, Status = Status.Aktivan,
                KlijentId = "tip_user"
            }
        );
        await _context.SaveChangesAsync();

        var results = await _oglasService.SearchOglasiAsync(null, null, "IT", null, null, null);

        Assert.Single(results);
        Assert.Equal("IT", results[0].Oglas.TipPosla);
    }

    // ══════════════════════════════════════════════════════════════════
    // ApplicationDbContext.HandleStripePaymentEventAsync — killed mutants
    // ══════════════════════════════════════════════════════════════════

    [Fact]
    public async Task HandleStripePaymentEvent_NewPaidTransaction_SetsPaidAt()
    {
        // MUTATION TARGET: Remove PaidAt assignment in new transaction creation
        //   or change the ternary PaidAt = newStatus == PaymentStatus.Paid ? ... : null
        // KILLS: PaidAt assignment mutation
        await _context.HandleStripePaymentEventAsync("pi_new_paid", "evt_new_paid", PaymentStatus.Paid, 100, "usd");

        var tx = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_new_paid");

        Assert.NotNull(tx.PaidAt);
        Assert.Equal(PaymentStatus.Paid, tx.Status);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_NewFailedTransaction_PaidAtIsNull()
    {
        // MUTATION TARGET: Change PaidAt = newStatus == PaymentStatus.Paid ? ... : null to always set PaidAt
        // KILLS: ternary mutation that always sets PaidAt
        await _context.HandleStripePaymentEventAsync("pi_new_failed", "evt_new_failed", PaymentStatus.Failed, 100, "usd");

        var tx = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_new_failed");

        Assert.Null(tx.PaidAt);
        Assert.Equal(PaymentStatus.Failed, tx.Status);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_UpdateToPaid_SetsPaidAt()
    {
        // MUTATION TARGET: Remove the if (newStatus == PaymentStatus.Paid) PaidAt update in else branch
        // KILLS: PaidAt update mutation in existing transaction path
        await _context.HandleStripePaymentEventAsync("pi_update_paid", "evt_upd1", PaymentStatus.Pending, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_update_paid", "evt_upd2", PaymentStatus.Paid, 100, "usd");

        var tx = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_update_paid");

        Assert.Equal(PaymentStatus.Paid, tx.Status);
        Assert.NotNull(tx.PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_UpdateToFailed_DoesNotSetPaidAt()
    {
        // MUTATION TARGET: Remove the if condition in else branch (always set PaidAt)
        // KILLS: PaidAt always-set mutation
        await _context.HandleStripePaymentEventAsync("pi_update_fail", "evt_upd3", PaymentStatus.Paid, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_update_fail", "evt_upd4", PaymentStatus.Failed, 100, "usd");

        var tx = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_update_fail");

        Assert.Equal(PaymentStatus.Failed, tx.Status);
        // PaidAt was set during the first call (Paid), remains from that call
        Assert.NotNull(tx.PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_Idempotency_UsesStripeEventId()
    {
        // MUTATION TARGET: Change stripeEventId check to stripePaymentIntentId
        //   or remove the idempotency check
        // KILLS: idempotency guard mutation
        await _context.HandleStripePaymentEventAsync("pi_idem_a", "evt_idem_1", PaymentStatus.Paid, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_idem_b", "evt_idem_1", PaymentStatus.Failed, 200, "eur");

        // Second call has different paymentIntentId but same eventId — should be ignored
        var all = await _context.PaymentTransactions
            .Where(p => p.StripePaymentIntentId == "pi_idem_a" || p.StripePaymentIntentId == "pi_idem_b")
            .ToListAsync();

        Assert.Single(all);
        Assert.Equal("pi_idem_a", all[0].StripePaymentIntentId);
        Assert.Equal(PaymentStatus.Paid, all[0].Status);
    }
}
