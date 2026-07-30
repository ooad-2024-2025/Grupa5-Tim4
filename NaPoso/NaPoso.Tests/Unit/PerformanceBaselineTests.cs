using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;
using Xunit;

namespace NaPoso.Tests;

public class PerformanceBaselineTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly StatisticsService _statisticsService;
    private readonly OglasService _oglasService;

    public PerformanceBaselineTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _statisticsService = new StatisticsService(_context);
        _oglasService = new OglasService(_context);
    }

    public void Dispose() => _context.Dispose();

    private async Task SeedDataAsync(int oglasCount = 100, int userCount = 50)
    {
        var users = Enumerable.Range(1, userCount)
            .Select(i => new Korisnik { Id = $"user{i}", UserName = $"user{i}@test.com", Email = $"user{i}@test.com" })
            .ToList();
        _context.Users.AddRange(users);

        var oglasi = Enumerable.Range(1, oglasCount)
            .Select(i => new Oglas
            {
                Naslov = $"Oglas {i}",
                Opis = $"Opis {i}",
                Lokacija = "Sarajevo",
                TipPosla = "IT",
                CijenaPosla = i * 100,
                Status = i % 3 == 0 ? Status.Aktivan : (i % 3 == 1 ? Status.Neaktivan : Status.Placen),
                KlijentId = $"user{i % userCount + 1}"
            })
            .ToList();
        _context.Oglas.AddRange(oglasi);

        var recenzije = Enumerable.Range(1, 20)
            .Select(i => new Recenzija { Ocjena = (i % 5) + 1, Sadrzaj = $"Recenzija {i}", KlijentId = $"user{i}", RadnikId = $"user{i + 20}" })
            .ToList();
        _context.Recenzija.AddRange(recenzije);

        await _context.SaveChangesAsync();
    }

    [Fact]
    public async Task StatisticsService_100Oglas_CompletesWithinThreshold()
    {
        await SeedDataAsync(oglasCount: 100, userCount: 50);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _statisticsService.GetStatisticsAsync();
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 5000, $"StatisticsService took {sw.ElapsedMilliseconds}ms (>5000ms threshold)");
        Assert.Equal(100, result.BrojPoslova);
    }

    [Fact]
    public async Task OglasService_Search_100Oglas_CompletesWithinThreshold()
    {
        await SeedDataAsync(oglasCount: 100);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await _oglasService.SearchOglasiAsync("Oglas", null, null, null, null, null);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 3000, $"SearchOglasi took {sw.ElapsedMilliseconds}ms (>3000ms threshold)");
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task OglasService_Pagination_ReturnsCorrectPageSize()
    {
        await SeedDataAsync(oglasCount: 50);

        var page1 = await _oglasService.GetAllOglasAsync(page: 1, pageSize: 10);
        var page2 = await _oglasService.GetAllOglasAsync(page: 2, pageSize: 10);
        var page6 = await _oglasService.GetAllOglasAsync(page: 6, pageSize: 10);

        Assert.Equal(10, page1.Count);
        Assert.Equal(10, page2.Count);
        Assert.Empty(page6);
        Assert.NotEqual(page1[0].Id, page2[0].Id);
    }

    [Fact]
    public async Task OglasService_Search_MaxPageSize_ClampedTo100()
    {
        await SeedDataAsync(oglasCount: 5);

        var result = await _oglasService.SearchOglasiAsync(null, null, null, null, null, null, page: 1, pageSize: 9999);
        Assert.True(result.Count <= 100);
    }
}
