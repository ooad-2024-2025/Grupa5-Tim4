using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests.Integration;

public class EdgeCaseTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public EdgeCaseTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    // ── Stripe Payment Edge Cases ──

    [Fact]
    public async Task HandleStripePaymentEvent_ConcurrentDuplicateEvents_OnlyOneProcessed()
    {
        await _context.HandleStripePaymentEventAsync("pi_dup", "evt_dup1", PaymentStatus.Paid, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_dup", "evt_dup1", PaymentStatus.Failed, 100, "usd");

        var transactions = await _context.PaymentTransactions
            .Where(p => p.StripePaymentIntentId == "pi_dup")
            .ToListAsync();

        Assert.Single(transactions);
        Assert.Equal(PaymentStatus.Paid, transactions[0].Status);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_ZeroAmount_Succeeds()
    {
        await _context.HandleStripePaymentEventAsync("pi_zero", "evt_zero", PaymentStatus.Paid, 0, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstAsync(p => p.StripePaymentIntentId == "pi_zero");

        Assert.Equal(0, transaction.Amount);
        Assert.NotNull(transaction.PaidAt);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_EmptyCurrency_Succeeds()
    {
        await _context.HandleStripePaymentEventAsync("pi_empty_curr", "evt_empty_curr", PaymentStatus.Paid, 100, "");

        var transaction = await _context.PaymentTransactions
            .FirstAsync(p => p.StripePaymentIntentId == "pi_empty_curr");

        Assert.Equal("", transaction.Currency);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_VeryLongPaymentIntentId_Succeeds()
    {
        var longId = new string('a', 1000);

        await _context.HandleStripePaymentEventAsync(longId, "evt_long_pi", PaymentStatus.Paid, 100, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstAsync(p => p.StripePaymentIntentId == longId);

        Assert.Equal(longId, transaction.StripePaymentIntentId);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_VeryLongEventId_Succeeds()
    {
        var longEventId = new string('b', 1000);

        await _context.HandleStripePaymentEventAsync("pi_long_evt", longEventId, PaymentStatus.Paid, 100, "usd");

        var transaction = await _context.PaymentTransactions
            .FirstAsync(p => p.StripeEventId == longEventId);

        Assert.Equal(longEventId, transaction.StripeEventId);
    }

    // ── Statistics Edge Cases ──

    [Fact]
    public async Task Statistics_WithMixedStatuses_ReturnsCorrectCounts()
    {
        _context.Oglas.AddRange(
            new Oglas { Naslov = "A1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan },
            new Oglas { Naslov = "A2", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan },
            new Oglas { Naslov = "N1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 300, Status = Status.Neaktivan },
            new Oglas { Naslov = "N2", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 400, Status = Status.Neaktivan },
            new Oglas { Naslov = "N3", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 500, Status = Status.Neaktivan },
            new Oglas { Naslov = "P1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 600, Status = Status.Placen },
            new Oglas { Naslov = "PH1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 700, Status = Status.Prihvacen }
        );
        await _context.SaveChangesAsync();

        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(7, stats.BrojPoslova);
        Assert.Equal(2 + 1, stats.AktivniPoslovi);
        Assert.Equal(3 + 1, stats.BrojZavrsenihPoslova);
        Assert.Equal(1, stats.PlaceniPoslovi);
        Assert.Equal(stats.BrojPoslova, stats.AktivniPoslovi + stats.BrojZavrsenihPoslova);
    }

    [Fact]
    public async Task Statistics_WithNoRecenzije_ReturnsZeroAverage()
    {
        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(0, stats.ProsjecnaOcjena);
    }

    [Fact]
    public async Task Statistics_WithSingleRecenzija_ReturnsThatRating()
    {
        _context.Recenzija.Add(
            new Recenzija { Ocjena = 4, Sadrzaj = "Good", KlijentId = "k1", RadnikId = "r1" }
        );
        await _context.SaveChangesAsync();

        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(4.0, stats.ProsjecnaOcjena, 1);
    }

    [Fact]
    public async Task Statistics_WithLargeDataset_ReturnsCorrectCounts()
    {
        var oglasList = Enumerable.Range(1, 50).Select(i => new Oglas
        {
            Naslov = $"Oglas {i}",
            Opis = $"Opis {i}",
            Lokacija = "Sarajevo",
            TipPosla = "IT",
            CijenaPosla = i * 100,
            Status = i <= 20 ? Status.Aktivan : i <= 40 ? Status.Neaktivan : Status.Placen
        }).ToList();

        _context.Oglas.AddRange(oglasList);

        var recenzijeList = Enumerable.Range(1, 10).Select(i => new Recenzija
        {
            Ocjena = i,
            Sadrzaj = $"Recenzija {i}",
            KlijentId = $"k{i}",
            RadnikId = $"r{i}"
        }).ToList();

        _context.Recenzija.AddRange(recenzijeList);
        await _context.SaveChangesAsync();

        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(50, stats.BrojPoslova);
        Assert.Equal(20, stats.AktivniPoslovi);
        Assert.Equal(20 + 10, stats.BrojZavrsenihPoslova);
        Assert.Equal(10, stats.PlaceniPoslovi);
        Assert.Equal(5.5, stats.ProsjecnaOcjena, 1);
        Assert.Equal(stats.BrojPoslova, stats.AktivniPoslovi + stats.BrojZavrsenihPoslova);
    }

    // ── Cascade Delete Edge Cases ──

    [Fact]
    public async Task OglasKorisnik_CascadeDelete_WhenOglasDeleted()
    {
        var oglas = new Oglas
        {
            Naslov = "Za brisanje", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var userId = Guid.NewGuid().ToString();
        var user = new Korisnik { Id = userId, UserName = "test@test.com", Email = "test@test.com" };
        _context.Users.Add(user);

        _context.OglasKorisnik.Add(new OglasKorisnik
        {
            OglasId = oglas.Id,
            KorisnikId = userId,
            DatumPrijave = DateTime.UtcNow,
            Status = Status.Aktivan
        });
        await _context.SaveChangesAsync();

        _context.Oglas.Remove(oglas);
        await _context.SaveChangesAsync();

        var remaining = await _context.OglasKorisnik
            .Where(ok => ok.OglasId == oglas.Id)
            .ToListAsync();

        Assert.Empty(remaining);
    }

    [Fact]
    public async Task Chat_Messages_CascadeDelete_WhenChatDeleted()
    {
        var user1 = new Korisnik { Id = "u1", UserName = "user1@test.com", Email = "user1@test.com" };
        var user2 = new Korisnik { Id = "u2", UserName = "user2@test.com", Email = "user2@test.com" };
        _context.Users.AddRange(user1, user2);

        var oglas = new Oglas
        {
            Naslov = "Chat Test", Opis = "Opis", Lokacija = "Sarajevo",
            TipPosla = "IT", CijenaPosla = 100
        };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var chat = new Chat
        {
            Korisnik1Id = "u1",
            Korisnik2Id = "u2",
            OglasId = oglas.Id,
            CreatedAt = DateTime.UtcNow
        };
        _context.Chat.Add(chat);
        await _context.SaveChangesAsync();

        _context.Poruka.AddRange(
            new Poruka { ChatId = chat.Id, PosiljaocId = "u1", Tekst = "Hello", PoslanoAt = DateTime.UtcNow },
            new Poruka { ChatId = chat.Id, PosiljaocId = "u2", Tekst = "Hi there", PoslanoAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        _context.Chat.Remove(chat);
        await _context.SaveChangesAsync();

        var remainingMessages = await _context.Poruka
            .Where(p => p.ChatId == chat.Id)
            .ToListAsync();

        Assert.Empty(remainingMessages);
    }
}
