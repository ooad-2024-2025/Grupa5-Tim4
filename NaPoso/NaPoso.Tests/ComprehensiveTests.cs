using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests;

public class ComprehensiveTests : IDisposable
{
    private readonly ApplicationDbContext _context;

    public ComprehensiveTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
    }

    public void Dispose() => _context.Dispose();

    // ── DateTime UTC Tests ──

    [Fact]
    public void OglasKorisnik_DefaultDatumPrijave_IsUtc()
    {
        var prijava = new OglasKorisnik();
        Assert.Equal(DateTimeKind.Utc, prijava.DatumPrijave.Kind);
    }

    [Fact]
    public void Poruka_DefaultPoslanoAt_IsUtc()
    {
        var poruka = new Poruka();
        Assert.Equal(DateTimeKind.Utc, poruka.PoslanoAt.Kind);
    }

    [Fact]
    public void Chat_DefaultCreatedAt_IsUtc()
    {
        var chat = new Chat();
        Assert.Equal(DateTimeKind.Utc, chat.CreatedAt.Kind);
    }

    [Fact]
    public async Task OglasKorisnik_SaveToDb_PreservesUtcKind()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik
        {
            OglasId = oglas.Id,
            KorisnikId = "user1",
            DatumPrijave = DateTime.UtcNow,
            Status = Status.Aktivan
        };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var saved = await _context.OglasKorisnik.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, saved.DatumPrijave.Kind);
    }

    // ── Oglas CRUD Tests ──

    [Fact]
    public async Task Oglas_Create_SetsDefaultStatus()
    {
        var oglas = new Oglas { Naslov = "Novi oglas", Opis = "Test opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 500 };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var saved = await _context.Oglas.FirstAsync();
        Assert.Equal(Status.Neaktivan, saved.Status);
        Assert.Equal("Novi oglas", saved.Naslov);
    }

    [Fact]
    public async Task Oglas_Update_Naslov()
    {
        var oglas = new Oglas { Naslov = "Stari", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100 };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        oglas.Naslov = "Novi";
        await _context.SaveChangesAsync();

        var saved = await _context.Oglas.FirstAsync();
        Assert.Equal("Novi", saved.Naslov);
    }

    [Fact]
    public async Task Oglas_Delete_RemovesFromDb()
    {
        var oglas = new Oglas { Naslov = "Za brisanje", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100 };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        _context.Oglas.Remove(oglas);
        await _context.SaveChangesAsync();

        Assert.Empty(await _context.Oglas.ToListAsync());
    }

    [Fact]
    public async Task Oglas_FilterByStatus_ReturnsCorrect()
    {
        _context.Oglas.AddRange(
            new Oglas { Naslov = "Aktivan", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan },
            new Oglas { Naslov = "Neaktivan", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Neaktivan },
            new Oglas { Naslov = "Placen", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 300, Status = Status.Placen }
        );
        await _context.SaveChangesAsync();

        var aktivni = await _context.Oglas.Where(o => o.Status == Status.Aktivan).ToListAsync();
        var placeni = await _context.Oglas.Where(o => o.Status == Status.Placen).ToListAsync();

        Assert.Single(aktivni);
        Assert.Single(placeni);
        Assert.Equal("Aktivan", aktivni[0].Naslov);
        Assert.Equal("Placen", placeni[0].Naslov);
    }

    // ── Notification Tests ──

    [Fact]
    public async Task Obavijest_Create_WithUtcTimestamp()
    {
        var obavijest = new Obavijest
        {
            KorisnikId = "user1",
            Sadrzaj = "Test notifikacija",
            VrijemeSlanja = DateTime.UtcNow,
            Tip = Obavjestenje.DrugaObavjestenja
        };
        _context.Obavijest.Add(obavijest);
        await _context.SaveChangesAsync();

        var saved = await _context.Obavijest.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, saved.VrijemeSlanja.Kind);
        Assert.Equal("Test notifikacija", saved.Sadrzaj);
    }

    [Fact]
    public async Task Obavijest_FilterByUserId_ReturnsOnlyUserNotifications()
    {
        _context.Obavijest.AddRange(
            new Obavijest { KorisnikId = "user1", Sadrzaj = "Za user1", VrijemeSlanja = DateTime.UtcNow, Tip = Obavjestenje.DrugaObavjestenja },
            new Obavijest { KorisnikId = "user2", Sadrzaj = "Za user2", VrijemeSlanja = DateTime.UtcNow, Tip = Obavjestenje.DrugaObavjestenja },
            new Obavijest { KorisnikId = "user1", Sadrzaj = "Druga za user1", VrijemeSlanja = DateTime.UtcNow, Tip = Obavjestenje.Email }
        );
        await _context.SaveChangesAsync();

        var user1Notifications = await _context.Obavijest
            .Where(o => o.KorisnikId == "user1")
            .ToListAsync();

        Assert.Equal(2, user1Notifications.Count);
        Assert.All(user1Notifications, n => Assert.Equal("user1", n.KorisnikId));
    }

    [Fact]
    public async Task Obavijest_MarkAsRead()
    {
        var obavijest = new Obavijest
        {
            KorisnikId = "user1",
            Sadrzaj = "Test",
            VrijemeSlanja = DateTime.UtcNow,
            Tip = Obavjestenje.DrugaObavjestenja,
            IsRead = false
        };
        _context.Obavijest.Add(obavijest);
        await _context.SaveChangesAsync();

        obavijest.IsRead = true;
        await _context.SaveChangesAsync();

        var saved = await _context.Obavijest.FirstAsync();
        Assert.True(saved.IsRead);
    }

    // ── Payment Transaction Tests ──

    [Fact]
    public async Task PaymentTransaction_UniqueIndex_StripePaymentIntentId()
    {
        await _context.HandleStripePaymentEventAsync("pi_unique", "evt1", PaymentStatus.Paid, 100, "usd");

        var count = await _context.PaymentTransactions
            .CountAsync(p => p.StripePaymentIntentId == "pi_unique");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task PaymentTransaction_UniqueIndex_StripeEventId()
    {
        await _context.HandleStripePaymentEventAsync("pi_1", "evt_unique", PaymentStatus.Paid, 100, "usd");

        var count = await _context.PaymentTransactions
            .CountAsync(p => p.StripeEventId == "evt_unique");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task PaymentTransaction_PaidAt_SetOnlyForPaid()
    {
        await _context.HandleStripePaymentEventAsync("pi_pending", "evt_p", PaymentStatus.Pending, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_paid", "evt_d", PaymentStatus.Paid, 200, "usd");
        await _context.HandleStripePaymentEventAsync("pi_failed", "evt_f", PaymentStatus.Failed, 300, "usd");

        var pending = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_pending");
        var paid = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_paid");
        var failed = await _context.PaymentTransactions.FirstAsync(p => p.StripePaymentIntentId == "pi_failed");

        Assert.Null(pending.PaidAt);
        Assert.NotNull(paid.PaidAt);
        Assert.Null(failed.PaidAt);
    }

    // ── Chat Tests ──

    [Fact]
    public async Task Chat_Create_WithUtcTimestamp()
    {
        var chat = new Chat
        {
            Korisnik1Id = "user1",
            Korisnik2Id = "user2",
            OglasId = 1,
            CreatedAt = DateTime.UtcNow
        };
        _context.Chat.Add(chat);
        await _context.SaveChangesAsync();

        var saved = await _context.Chat.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, saved.CreatedAt.Kind);
    }

    [Fact]
    public async Task Poruka_Create_WithUtcTimestamp()
    {
        var chat = new Chat { Korisnik1Id = "u1", Korisnik2Id = "u2", OglasId = 1, CreatedAt = DateTime.UtcNow };
        _context.Chat.Add(chat);
        await _context.SaveChangesAsync();

        var poruka = new Poruka
        {
            ChatId = chat.Id,
            PosiljaocId = "user1",
            Tekst = "Hello",
            PoslanoAt = DateTime.UtcNow
        };
        _context.Poruka.Add(poruka);
        await _context.SaveChangesAsync();

        var saved = await _context.Poruka.FirstAsync();
        Assert.Equal(DateTimeKind.Utc, saved.PoslanoAt.Kind);
        Assert.Equal("Hello", saved.Tekst);
    }

    // ── Statistics Tests ──

    [Fact]
    public async Task Statistics_EmptyDb_ReturnsZeros()
    {
        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(0, stats.BrojKorisnika);
        Assert.Equal(0, stats.BrojPoslova);
        Assert.Equal(0, stats.BrojKlijenata);
        Assert.Equal(0, stats.BrojRadnika);
        Assert.Equal(0, stats.BrojZavrsenihPoslova);
        Assert.Equal(0, stats.AktivniPoslovi);
        Assert.Equal(0, stats.PlaceniPoslovi);
    }

    [Fact]
    public async Task Statistics_CountsByStatus()
    {
        _context.Oglas.AddRange(
            new Oglas { Naslov = "A1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan },
            new Oglas { Naslov = "A2", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan },
            new Oglas { Naslov = "N1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 300, Status = Status.Neaktivan },
            new Oglas { Naslov = "P1", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 400, Status = Status.Placen }
        );
        await _context.SaveChangesAsync();

        var service = new StatisticsService(_context);
        var stats = await service.GetStatisticsAsync();

        Assert.Equal(4, stats.BrojPoslova);
        Assert.Equal(2, stats.AktivniPoslovi);
        Assert.Equal(1 + 1, stats.BrojZavrsenihPoslova);
        Assert.Equal(1, stats.PlaceniPoslovi);
        Assert.Equal(stats.BrojPoslova, stats.AktivniPoslovi + stats.BrojZavrsenihPoslova);
    }

    // ── OglasKorisnik (Job Application) Tests ──

    [Fact]
    public async Task OglasKorisnik_Create_PreservesUtc()
    {
        var oglas = new Oglas { Naslov = "Posao", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik
        {
            OglasId = oglas.Id,
            KorisnikId = "radnik1",
            DatumPrijave = DateTime.UtcNow,
            Status = Status.Aktivan
        };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var saved = await _context.OglasKorisnik.FirstAsync();
        Assert.Equal(Status.Aktivan, saved.Status);
        Assert.Equal(DateTimeKind.Utc, saved.DatumPrijave.Kind);
    }

    [Fact]
    public async Task OglasKorisnik_DuplicateApplication_Prevented()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        _context.OglasKorisnik.Add(new OglasKorisnik { OglasId = oglas.Id, KorisnikId = "user1", DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan });
        await _context.SaveChangesAsync();

        var exists = await _context.OglasKorisnik
            .AnyAsync(ok => ok.OglasId == oglas.Id && ok.KorisnikId == "user1");
        Assert.True(exists);
    }

    // ── StripeService Tests ──

    [Fact]
    public void PaymentStatus_Enum_ContainsAllValues()
    {
        Assert.Equal(0, (int)PaymentStatus.Pending);
        Assert.Equal(1, (int)PaymentStatus.Paid);
        Assert.Equal(2, (int)PaymentStatus.Failed);
        Assert.Equal(3, (int)PaymentStatus.Refunded);
    }

    [Fact]
    public void Status_Enum_AllValues()
    {
        Assert.Equal(0, (int)Status.Neaktivan);
        Assert.Equal(1, (int)Status.Aktivan);
        Assert.Equal(2, (int)Status.Prihvacen);
        Assert.Equal(4, (int)Status.Placen);
        Assert.Equal(5, (int)Status.Zavrsen);
    }

    [Fact]
    public void Obavjestenje_Enum_ContainsEmailAndDruga()
    {
        Assert.True(Enum.IsDefined(typeof(Obavjestenje), Obavjestenje.Email));
        Assert.True(Enum.IsDefined(typeof(Obavjestenje), Obavjestenje.DrugaObavjestenja));
    }

    // ── Domain Model Validation Tests ──

    [Fact]
    public void Oglas_DefaultProperties()
    {
        var oglas = new Oglas();
        Assert.Equal(Status.Neaktivan, oglas.Status);
        Assert.Null(oglas.KlijentId);
        Assert.Null(oglas.RadnikId);
    }

    [Fact]
    public void Recenzija_DefaultValues()
    {
        var recenzija = new Recenzija();
        Assert.Equal(0, recenzija.Ocjena);
        Assert.Null(recenzija.Sadrzaj);
    }

    [Fact]
    public void PaymentTransaction_DefaultValues()
    {
        var pt = new PaymentTransaction();
        Assert.Equal(PaymentStatus.Pending, pt.Status);
        Assert.Equal("usd", pt.Currency);
        Assert.Equal(0, pt.Amount);
        Assert.Null(pt.PaidAt);
    }

    [Fact]
    public void Statistika_DefaultValues()
    {
        var s = new Statistika();
        Assert.Equal(0, s.BrojKorisnika);
        Assert.Equal(0, s.BrojPoslova);
        Assert.Equal(0, s.BrojKlijenata);
        Assert.Equal(0, s.BrojRadnika);
        Assert.Equal(0, s.BrojZavrsenihPoslova);
        Assert.Equal(0, s.AktivniPoslovi);
        Assert.Equal(0, s.PlaceniPoslovi);
        Assert.Equal(0, s.ProsjecnaOcjena);
    }

    // ── Edge Cases ──

    [Fact]
    public async Task HandleStripePaymentEvent_MultipleDifferentIntents()
    {
        await _context.HandleStripePaymentEventAsync("pi_1", "evt_1", PaymentStatus.Paid, 100, "usd");
        await _context.HandleStripePaymentEventAsync("pi_2", "evt_2", PaymentStatus.Failed, 200, "eur");
        await _context.HandleStripePaymentEventAsync("pi_3", "evt_3", PaymentStatus.Refunded, 300, "gbp");

        var all = await _context.PaymentTransactions.ToListAsync();
        Assert.Equal(3, all.Count);
        Assert.Contains(all, t => t.Status == PaymentStatus.Paid);
        Assert.Contains(all, t => t.Status == PaymentStatus.Failed);
        Assert.Contains(all, t => t.Status == PaymentStatus.Refunded);
    }

    [Fact]
    public async Task HandleStripePaymentEvent_AmountPreserved()
    {
        await _context.HandleStripePaymentEventAsync("pi_amount", "evt_amount", PaymentStatus.Paid, 99999, "eur");

        var saved = await _context.PaymentTransactions.FirstAsync();
        Assert.Equal(99999, saved.Amount);
        Assert.Equal("eur", saved.Currency);
    }
}
