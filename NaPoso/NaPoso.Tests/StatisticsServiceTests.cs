using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Tests;

public class StatisticsServiceTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatisticsService _service;

    public StatisticsServiceTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);

        _service = new StatisticsService(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetStatistics_ReturnsZeroes_WhenEmpty()
    {
        var result = await _service.GetStatisticsAsync();

        Assert.Equal(0, result.BrojKorisnika);
        Assert.Equal(0, result.BrojPoslova);
        Assert.Equal(0, result.BrojZavrsenihPoslova);
        Assert.Equal(0, result.AktivniPoslovi);
        Assert.Equal(0, result.PlaceniPoslovi);
        Assert.Equal(0, result.ProsjecnaOcjena);
    }

    [Fact]
    public async Task GetStatistics_CountsOglasCorrectly()
    {
        _context.Oglas.AddRange(
            new Oglas { Naslov = "Test 1", Opis = "Opis 1", Lokacija = "Sarajevo", TipPosla = "IT", CijenaPosla = 100, Status = Status.Aktivan },
            new Oglas { Naslov = "Test 2", Opis = "Opis 2", Lokacija = "Banja Luka", TipPosla = "Građevina", CijenaPosla = 200, Status = Status.Neaktivan },
            new Oglas { Naslov = "Test 3", Opis = "Opis 3", Lokacija = "Tuzla", TipPosla = "IT", CijenaPosla = 300, Status = Status.Placen }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetStatisticsAsync();

        Assert.Equal(3, result.BrojPoslova);
        Assert.Equal(1 + 1, result.BrojZavrsenihPoslova);
        Assert.Equal(1, result.AktivniPoslovi);
        Assert.Equal(1, result.PlaceniPoslovi);
        Assert.Equal(result.BrojPoslova, result.AktivniPoslovi + result.BrojZavrsenihPoslova);
    }

    [Fact]
    public async Task GetStatistics_CalculatesAverageRating()
    {
        _context.Recenzija.AddRange(
            new Recenzija { Ocjena = 5, Sadrzaj = "Odlično", KlijentId = "k1", RadnikId = "r1" },
            new Recenzija { Ocjena = 3, Sadrzaj = "OK", KlijentId = "k2", RadnikId = "r2" },
            new Recenzija { Ocjena = 4, Sadrzaj = "Dobro", KlijentId = "k3", RadnikId = "r3" }
        );
        await _context.SaveChangesAsync();

        var result = await _service.GetStatisticsAsync();

        Assert.Equal(4.0, result.ProsjecnaOcjena, 1);
    }
}
