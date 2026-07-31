using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Models;

namespace NaPoso.Data
{
    public static class DatabaseSeeder
    {
        public static async Task<bool> SeedAsync(IServiceProvider serviceProvider)
        {
            var context = serviceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = serviceProvider.GetRequiredService<UserManager<Korisnik>>();
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure roles exist
            string[] roles = { RoleConstants.Klijent, RoleConstants.Radnik, RoleConstants.Admin };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // Check if we already have seeded data
            if (await context.Oglas.AnyAsync() || await userManager.FindByEmailAsync("klijent1@mail.com") != null)
            {
                return false; // Seeding already done
            }

            // Seed Klijenti (10)
            var klijentiData = new[]
            {
                new { Ime = "Emir", Prezime = "Hadžić", Grad = "Sarajevo" },
                new { Ime = "Lejla", Prezime = "Kovačević", Grad = "Tuzla" },
                new { Ime = "Tarik", Prezime = "Dedić", Grad = "Zenica" },
                new { Ime = "Amina", Prezime = "Hodžić", Grad = "Mostar" },
                new { Ime = "Haris", Prezime = "Spahić", Grad = "Bihać" },
                new { Ime = "Nedim", Prezime = "Ibrahimović", Grad = "Sarajevo" },
                new { Ime = "Selma", Prezime = "Beganović", Grad = "Banja Luka" },
                new { Ime = "Kenan", Prezime = "Husić", Grad = "Tuzla" },
                new { Ime = "Emina", Prezime = "Bašić", Grad = "Mostar" },
                new { Ime = "Adnan", Prezime = "Delić", Grad = "Zenica" }
            };

            var klijenti = new List<Korisnik>();
            for (int i = 0; i < klijentiData.Length; i++)
            {
                var klijent = new Korisnik
                {
                    UserName = $"klijent{i + 1}@mail.com",
                    Email = $"klijent{i + 1}@mail.com",
                    Ime = klijentiData[i].Ime,
                    Prezime = klijentiData[i].Prezime,
                    EmailConfirmed = true,
                    Verified = true
                };

                var result = await userManager.CreateAsync(klijent, "Test1234!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(klijent, RoleConstants.Klijent);
                    klijenti.Add(klijent);
                }
            }

            // Seed Radnici (15)
            var radniciData = new[]
            {
                new { Ime = "Amar", Prezime = "Karić" },
                new { Ime = "Sara", Prezime = "Tomić" },
                new { Ime = "Dino", Prezime = "Alić" },
                new { Ime = "Nina", Prezime = "Mujić" },
                new { Ime = "Vedad", Prezime = "Halilović" },
                new { Ime = "Zana", Prezime = "Mehmedović" },
                new { Ime = "Faris", Prezime = "Omerović" },
                new { Ime = "Ilhana", Prezime = "Topčagić" },
                new { Ime = "Denis", Prezime = "Pervan" },
                new { Ime = "Mia", Prezime = "Čengić" },
                new { Ime = "Rijad", Prezime = "Salihović" },
                new { Ime = "Naida", Prezime = "Imamović" },
                new { Ime = "Aldin", Prezime = "Đipa" },
                new { Ime = "Amna", Prezime = "Šabanović" },
                new { Ime = "Tarik", Prezime = "Memić" }
            };

            var radnici = new List<Korisnik>();
            for (int i = 0; i < radniciData.Length; i++)
            {
                var radnik = new Korisnik
                {
                    UserName = $"radnik{i + 1}@mail.com",
                    Email = $"radnik{i + 1}@mail.com",
                    Ime = radniciData[i].Ime,
                    Prezime = radniciData[i].Prezime,
                    EmailConfirmed = true,
                    Verified = i % 2 == 0 // Half are verified
                };

                var result = await userManager.CreateAsync(radnik, "Test1234!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(radnik, RoleConstants.Radnik);
                    radnici.Add(radnik);
                }
            }

            // Fetch the test users created in Program.cs just in case
            var testKlijent = await userManager.FindByEmailAsync("klijent@mail.com");
            if (testKlijent != null) klijenti.Add(testKlijent);

            var testRadnik = await userManager.FindByEmailAsync("radnik@mail.com");
            if (testRadnik != null) radnici.Add(testRadnik);

            if (!klijenti.Any() || !radnici.Any())
                return;

            // Seed Oglasi (30+)
            var random = new Random(42); // Seed for reproducible data
            var tipoviPosla = new[] { "Fizicki poslovi", "Vodoinstalater", "IT i programiranje", "Ciscenje", "Edukacija", "Elektricar", "Prevodjenje", "Dizajn", "Vrtlarstvo", "Fotografija" };
            var lokacije = new[] { "Sarajevo", "Tuzla", "Zenica", "Mostar", "Banja Luka", "Bihać", "Travnik", "Bugojno", "Doboj", "Konjic" };
            
            var oglasi = new List<Oglas>();
            for (int i = 0; i < 35; i++)
            {
                var tipPosla = tipoviPosla[random.Next(tipoviPosla.Length)];
                var lokacija = lokacije[random.Next(lokacije.Length)];
                var klijent = klijenti[random.Next(klijenti.Count)];
                var status = (Enums.Enums.Status)random.Next(0, 4); // Aktivan, Zavrsen, Otkazan, Placen
                
                var oglas = new Oglas
                {
                    KlijentId = klijent.Id,
                    Naslov = $"Hitno: {tipPosla} u {lokacija}",
                    Opis = $"Tražimo pouzdanu osobu za {tipPosla.ToLower()} posao u {lokacija}.",
                    Lokacija = lokacija,
                    TipPosla = tipPosla,
                    CijenaPosla = random.Next(20, 1000),
                    Status = status
                };

                // Assign a worker if ad is completed or paid
                if (status == Enums.Enums.Status.Zavrsen || status == Enums.Enums.Status.Placen)
                {
                    oglas.RadnikId = radnici[random.Next(radnici.Count)].Id;
                }

                oglasi.Add(oglas);
            }

            await context.Oglas.AddRangeAsync(oglasi);
            await context.SaveChangesAsync();

            // Seed OglasKorisnik (Applications)
            var prijave = new List<OglasKorisnik>();
            foreach (var oglas in oglasi)
            {
                // Number of applicants for this ad
                int numApplicants = random.Next(0, 6);
                
                // Shuffle workers
                var shuffledWorkers = radnici.OrderBy(x => random.Next()).Take(numApplicants).ToList();
                
                foreach (var worker in shuffledWorkers)
                {
                    // Default application status
                    var prijavaStatus = Enums.Enums.Status.Aktivan;
                    
                    // If ad is finished/paid, and this worker is the chosen one
                    if (oglas.RadnikId == worker.Id)
                    {
                        prijavaStatus = oglas.Status;
                    }
                    else if (oglas.Status != Enums.Enums.Status.Aktivan)
                    {
                        prijavaStatus = Enums.Enums.Status.Neaktivan; // Ostale prijave nisu odabrane
                    }

                    prijave.Add(new OglasKorisnik
                    {
                        OglasId = oglas.Id,
                        KorisnikId = worker.Id,
                        Status = prijavaStatus,
                        DatumPrijave = DateTime.UtcNow.AddDays(-random.Next(1, 30))
                    });
                }
            }

            await context.OglasKorisnik.AddRangeAsync(prijave);
            await context.SaveChangesAsync();

            // Seed Recenzije
            var recenzije = new List<Recenzija>();
            foreach (var oglas in oglasi.Where(o => o.Status == Enums.Enums.Status.Zavrsen || o.Status == Enums.Enums.Status.Placen))
            {
                if (oglas.RadnikId != null && random.NextDouble() > 0.3) // 70% chance of leaving a review
                {
                    recenzije.Add(new Recenzija
                    {
                        KlijentId = oglas.KlijentId!,
                        RadnikId = oglas.RadnikId,
                        Ocjena = random.Next(3, 6), // Ratings between 3 and 5
                        Sadrzaj = "Super odrađen posao! Sve je bilo po dogovoru. Preporučujem ovog radnika za buduće saradnje."
                    });
                }
            }

            await context.Recenzija.AddRangeAsync(recenzije);
            await context.SaveChangesAsync();

            // Seed PaymentTransactions for earnings calculation
            var transactions = new List<PaymentTransaction>();
            int txIndex = 1;
            foreach (var oglas in oglasi.Where(o => o.Status == Enums.Enums.Status.Zavrsen || o.Status == Enums.Enums.Status.Placen))
            {
                if (oglas.RadnikId != null)
                {
                    long amountCents = (long)(oglas.CijenaPosla * 100);
                    long feeAmount = (long)(amountCents * 0.10); // 10%
                    
                    transactions.Add(new PaymentTransaction
                    {
                        UserId = oglas.KlijentId!,
                        WorkerUserId = oglas.RadnikId,
                        OglasId = oglas.Id,
                        Amount = amountCents,
                        PlatformFeeAmount = feeAmount,
                        Currency = "bam",
                        Status = PaymentStatus.Released,
                        StripePaymentIntentId = "pi_seed_" + txIndex,
                        StripeEventId = "evt_seed_" + txIndex,
                        CreatedAt = DateTime.UtcNow.AddDays(-random.Next(1, 10)),
                        PaidAt = DateTime.UtcNow.AddDays(-random.Next(0, 5))
                    });
                    txIndex++;
                }
            }

            await context.PaymentTransactions.AddRangeAsync(transactions);
            await context.SaveChangesAsync();

            return true;
        }
    }
}
