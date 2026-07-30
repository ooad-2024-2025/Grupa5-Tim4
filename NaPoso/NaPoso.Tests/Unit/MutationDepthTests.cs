using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;
using Xunit;

namespace NaPoso.Tests;

public class MutationDepthTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OglasService _service;

    public MutationDepthTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new OglasService(_context);
    }

    public void Dispose() => _context.Dispose();

    // Kill mutation: Status != Aktivan should reject
    [Fact]
    public async Task ApplyToOglas_WhenStatusNeaktivan_ReturnsFalse()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Neaktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _service.ApplyToOglasAsync(oglas.Id, "user1");
        Assert.False(result);
    }

    // Kill mutation: RadnikId != null should reject
    [Fact]
    public async Task ApplyToOglas_WhenRadnikAssigned_ReturnsFalse()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, RadnikId = "existing" };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _service.ApplyToOglasAsync(oglas.Id, "user1");
        Assert.False(result);
    }

    // Kill mutation: duplicate application check
    [Fact]
    public async Task ApplyToOglas_WhenAlreadyApplied_ReturnsFalse()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        _context.OglasKorisnik.Add(new OglasKorisnik { OglasId = oglas.Id, KorisnikId = "user1", DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan });
        await _context.SaveChangesAsync();

        var result = await _service.ApplyToOglasAsync(oglas.Id, "user1");
        Assert.False(result);
    }

    // Kill mutation: RejectApplication ownership check
    [Fact]
    public async Task RejectApplication_WhenNotOwner_ReturnsFalse()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "owner1" };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik { OglasId = oglas.Id, KorisnikId = "worker1", DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var result = await _service.RejectApplicationAsync(prijava.Id, "wrong_owner");
        Assert.False(result);
    }

    // Kill mutation: AcceptApplication sets Prihvacen status
    [Fact]
    public async Task AcceptApplication_SetsStatusToPrihvacen()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var prijava = new OglasKorisnik { OglasId = oglas.Id, KorisnikId = "worker1", DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan };
        _context.OglasKorisnik.Add(prijava);
        await _context.SaveChangesAsync();

        var result = await _service.AcceptApplicationAsync(prijava.Id);
        Assert.True(result);

        var updated = await _context.OglasKorisnik.FindAsync(prijava.Id);
        Assert.Equal(Status.Prihvacen, updated!.Status);
    }

    // Kill mutation: SearchOglasi sort default
    [Fact]
    public async Task SearchOglasi_DefaultSort_OrdersByNaslov()
    {
        _context.Users.Add(new Korisnik { Id = "k1", UserName = "k1@test.com", Email = "k1@test.com" });
        _context.Oglas.AddRange(
            new Oglas { Naslov = "Zebra", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "k1" },
            new Oglas { Naslov = "Alpha", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan, KlijentId = "k1" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.SearchOglasiAsync(null, null, null, null, null, null);
        Assert.Equal("Alpha", result[0].Oglas.Naslov);
        Assert.Equal("Zebra", result[1].Oglas.Naslov);
    }

    // Kill mutation: SearchOglasi price boundary >= vs >
    [Fact]
    public async Task SearchOglasi_MinCijena_IncludesExactMatch()
    {
        _context.Users.Add(new Korisnik { Id = "k1", UserName = "k1@test.com", Email = "k1@test.com" });
        _context.Oglas.Add(new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "k1" });
        await _context.SaveChangesAsync();

        var result = await _service.SearchOglasiAsync(null, null, null, null, 100, null);
        Assert.Single(result);
    }

    // Kill mutation: SearchOglasi maxCijena boundary
    [Fact]
    public async Task SearchOglasi_MaxCijena_IncludesExactMatch()
    {
        _context.Users.Add(new Korisnik { Id = "k1", UserName = "k1@test.com", Email = "k1@test.com" });
        _context.Oglas.Add(new Oglas { Naslov = "Test", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "k1" });
        await _context.SaveChangesAsync();

        var result = await _service.SearchOglasiAsync(null, null, null, null, null, 100);
        Assert.Single(result);
    }

    // Kill mutation: SearchOglasi empty search returns all
    [Fact]
    public async Task SearchOglasi_EmptySearch_ReturnsAll()
    {
        _context.Users.Add(new Korisnik { Id = "k1", UserName = "k1@test.com", Email = "k1@test.com" });
        _context.Oglas.AddRange(
            new Oglas { Naslov = "A", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "k1" },
            new Oglas { Naslov = "B", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 200, Status = Status.Aktivan, KlijentId = "k1" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.SearchOglasiAsync(null, null, null, null, null, null);
        Assert.Equal(2, result.Count);
    }

    // Kill mutation: SearchOglasi tipPosla filter
    [Fact]
    public async Task SearchOglasi_WithTipPosla_FiltersCorrectly()
    {
        _context.Users.Add(new Korisnik { Id = "k1", UserName = "k1@test.com", Email = "k1@test.com" });
        _context.Oglas.AddRange(
            new Oglas { Naslov = "A", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan, KlijentId = "k1" },
            new Oglas { Naslov = "B", Opis = "Opis", Lokacija = "Sarajevo", TipPosla = "Građevina", CijenaPosla = 200, Status = Status.Aktivan, KlijentId = "k1" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.SearchOglasiAsync(null, null, "IT", null, null, null);
        Assert.Single(result);
        Assert.Equal("IT", result[0].Oglas.TipPosla);
    }
}
