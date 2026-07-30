using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;
using Xunit;

namespace NaPoso.Tests;

public class OglasServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly OglasService _service;

    public OglasServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new OglasService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetOglasByIdAsync_ReturnsOglas_WhenExists()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "", Lokacija = "", TipPosla = "", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _service.GetOglasByIdAsync(oglas.Id);
        Assert.NotNull(result);
        Assert.Equal("Test", result.Naslov);
    }

    [Fact]
    public async Task GetOglasByIdAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.GetOglasByIdAsync(99999);
        Assert.Null(result);
    }

    [Fact]
    public async Task CreateOglasAsync_SetsKlijentIdAndStatus()
    {
        var oglas = new Oglas { Naslov = "New", Opis = "", Lokacija = "", TipPosla = "", CijenaPosla = 100 };
        var result = await _service.CreateOglasAsync(oglas, "user123", RoleConstants.Klijent);

        Assert.Equal("user123", result.KlijentId);
        Assert.Equal(Status.Aktivan, result.Status);
        Assert.Null(result.RadnikId);
    }

    [Fact]
    public async Task UpdateOglasAsync_UpdatesFields()
    {
        var oglas = new Oglas { Naslov = "Old", Opis = "Old", Lokacija = "Old", TipPosla = "Old", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var input = new Oglas { Naslov = "New", Opis = "New", Lokacija = "New", TipPosla = "New", CijenaPosla = 200 };
        var result = await _service.UpdateOglasAsync(oglas.Id, input);

        Assert.NotNull(result);
        Assert.Equal("New", result.Naslov);
        Assert.Equal(200, result.CijenaPosla);
    }

    [Fact]
    public async Task UpdateOglasAsync_ReturnsNull_WhenNotExists()
    {
        var result = await _service.UpdateOglasAsync(99999, new Oglas());
        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteOglasAsync_RemovesOglas()
    {
        var oglas = new Oglas { Naslov = "Delete me", Opis = "", Lokacija = "", TipPosla = "", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        var result = await _service.DeleteOglasAsync(oglas.Id);
        Assert.True(result);
        Assert.Null(await _service.GetOglasByIdAsync(oglas.Id));
    }

    [Fact]
    public async Task DeleteOglasAsync_ReturnsFalse_WhenNotExists()
    {
        var result = await _service.DeleteOglasAsync(99999);
        Assert.False(result);
    }

    [Fact]
    public async Task OglasExistsAsync_ReturnsTrue_WhenExists()
    {
        var oglas = new Oglas { Naslov = "Exists", Opis = "", Lokacija = "", TipPosla = "", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        Assert.True(await _service.OglasExistsAsync(oglas.Id));
    }

    [Fact]
    public async Task OglasExistsAsync_ReturnsFalse_WhenNotExists()
    {
        Assert.False(await _service.OglasExistsAsync(99999));
    }

    [Fact]
    public async Task GetPrijavljeniOglasiAsync_ReturnsAppliedOglasIds()
    {
        var oglas = new Oglas { Naslov = "Test", Opis = "", Lokacija = "", TipPosla = "", CijenaPosla = 100, Status = Status.Aktivan };
        _context.Oglas.Add(oglas);
        await _context.SaveChangesAsync();

        _context.OglasKorisnik.Add(new OglasKorisnik { OglasId = oglas.Id, KorisnikId = "user1", DatumPrijave = DateTime.UtcNow, Status = Status.Aktivan });
        await _context.SaveChangesAsync();

        var result = await _service.GetPrijavljeniOglasiAsync("user1");
        Assert.Single(result);
        Assert.Contains(oglas.Id, result);
    }

    [Fact]
    public async Task GetPrijavljeniOglasiAsync_ReturnsEmpty_WhenNoApplications()
    {
        var result = await _service.GetPrijavljeniOglasiAsync("nonexistent");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetApplicantsForOglasAsync_ReturnsEmpty_WhenOglasNotFound()
    {
        var result = await _service.GetApplicantsForOglasAsync(99999, "user1");
        Assert.Empty(result);
    }
}
