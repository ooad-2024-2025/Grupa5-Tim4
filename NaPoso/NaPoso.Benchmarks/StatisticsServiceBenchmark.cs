using BenchmarkDotNet.Attributes;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Benchmarks;

[MemoryDiagnoser]
public class StatisticsServiceBenchmark
{
    private ApplicationDbContext _context = null!;
    private StatisticsService _service = null!;

    [Params(10, 100, 1000)]
    public int OglasCount { get; set; }

    [GlobalSetup]
    public async Task Setup()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new ApplicationDbContext(options);
        _service = new StatisticsService(_context);

        // Seed data
        var users = Enumerable.Range(1, Math.Max(OglasCount / 5, 10))
            .Select(i => new Korisnik { Id = $"user{i}", UserName = $"user{i}@test.com", Email = $"user{i}@test.com" })
            .ToList();
        _context.Users.AddRange(users);

        var oglasi = Enumerable.Range(1, OglasCount)
            .Select(i => new Oglas
            {
                Naslov = $"Oglas {i}",
                Opis = $"Opis {i}",
                Lokacija = "Sarajevo",
                TipPosla = "IT",
                CijenaPosla = i * 100,
                Status = i % 3 == 0 ? Status.Aktivan : Status.Neaktivan,
                KlijentId = $"user{i % Math.Max(OglasCount / 5, 10) + 1}"
            })
            .ToList();
        _context.Oglas.AddRange(oglasi);

        var recenzije = Enumerable.Range(1, Math.Min(OglasCount / 5, 50))
            .Select(i => new Recenzija { Ocjena = (i % 5) + 1, Sadrzaj = $"Recenzija {i}", KlijentId = $"user{i}", RadnikId = $"user{i + 20}" })
            .ToList();
        _context.Recenzija.AddRange(recenzije);

        await _context.SaveChangesAsync();
    }

    [Benchmark]
    public async Task<NaPoso.Models.Statistika> GetStatistics()
    {
        return await _service.GetStatisticsAsync();
    }
}
