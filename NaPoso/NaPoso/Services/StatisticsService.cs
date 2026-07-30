using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using static NaPoso.Enums.Enums;

namespace NaPoso.Services
{
    public interface IStatisticsService
    {
        Task<Statistika> GetStatisticsAsync();
        Task SeedDataAsync();
    }

    public class StatisticsService : IStatisticsService
    {
        private readonly ApplicationDbContext _context;

        public StatisticsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Statistika> GetStatisticsAsync()
        {
            var totalUsers = await _context.Users.CountAsync();

            var sviOglasi = await _context.Oglas.ToListAsync();
            var totalJobs = sviOglasi.Count;

            // Konzistentno sa OglasStatusHelper i UI-em: Aktivni = u toku; Završeni = gotovi.
            // Aktivni + Završeni MORAJU dati BrojPoslova (svi statusi su pokriveni, bez "rupu").
            var activeJobs = sviOglasi.Count(o => o.Status == Status.Aktivan || o.Status == Status.Prihvacen);
            var finishedJobs = sviOglasi.Count(o => o.Status == Status.Neaktivan || o.Status == Status.Placen || o.Status == Status.Zavrsen);
            var paidJobs = sviOglasi.Count(o => o.Status == Status.Placen);

            var roleCounts = await _context.UserRoles
                .Join(_context.Roles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur, r })
                .GroupBy(x => x.r.Name)
                .Select(g => new { Role = g.Key, Count = g.Count() })
                .ToListAsync();

            var totalClients = roleCounts.FirstOrDefault(r => r.Role == RoleConstants.Klijent)?.Count ?? 0;
            var totalWorkers = roleCounts.FirstOrDefault(r => r.Role == RoleConstants.Radnik)?.Count ?? 0;

            var averageRating = await _context.Recenzija.AnyAsync()
                ? await _context.Recenzija.AverageAsync(r => r.Ocjena)
                : 0;

            return new Statistika
            {
                BrojKorisnika = totalUsers,
                BrojPoslova = totalJobs,
                BrojKlijenata = totalClients,
                BrojRadnika = totalWorkers,
                BrojZavrsenihPoslova = finishedJobs,
                PlaceniPoslovi = paidJobs,
                AktivniPoslovi = activeJobs,
                ProsjecnaOcjena = Math.Round(averageRating, 1)
            };
        }

        public async Task SeedDataAsync()
        {
            // Seed 5 fake users
            for (int i = 0; i < 5; i++)
            {
                var radnik = new Korisnik
                {
                    UserName = $"radnik{i}@mail.com",
                    Email = $"radnik{i}@mail.com",
                    EmailConfirmed = true,
                    Ime = $"Ime{i}",
                    Prezime = $"Prezime{i}"
                };
                if (!await _context.Users.AnyAsync(u => u.Email == radnik.Email))
                {
                    _context.Users.Add(radnik);
                }
            }
            await _context.SaveChangesAsync();
        }
    }
}
