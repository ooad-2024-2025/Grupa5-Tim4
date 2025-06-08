using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NaPoso.Data;
using NaPoso.Models;
using static NaPoso.Enums.Enums;

namespace NaPoso.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const string V = "Neaktivan";
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager; // Added UserManager for user role management  

        public AdminController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager; // Initialize UserManager  
        }
        public IActionResult Documents()
        {
            var documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
            if (!Directory.Exists(documentsPath))
                Directory.CreateDirectory(documentsPath);

            var files = Directory.GetFiles(documentsPath);

            // Dohvati listu odobrenih dokumenata iz baze
            var approvedFiles = _context.OdobreniDokumenti.Select(a => a.FileName).ToList();

            var dokumenti = new List<DokumentiKorisnika>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);

                // Preskoči fajlove koje je admin već prihvatio
                if (approvedFiles.Contains(fileName))
                    continue;

                var userId = fileName.Split('_')[0];
                var korisnik = _userManager.FindByIdAsync(userId).Result;

                dokumenti.Add(new DokumentiKorisnika
                {
                    FileName = fileName,
                    Korisnik = korisnik
                });
            }

            return View(dokumenti);
        }
        public IActionResult Index()
        {

            var totalUsers = _context.Users.Count();
            var totalJobs = _context.Oglas.Count();
            var finishedJobs = _context.Oglas.Count(o => o.Status == Status.Neaktivan);

            var users = _context.Users.ToList();
            var totalClients = users.Count(u => _userManager.GetRolesAsync((Korisnik)u).Result.Contains("Klijent"));
            var totalWorkers = users.Count(u => _userManager.GetRolesAsync((Korisnik)u).Result.Contains("Radnik"));

            var model = new Statistika
            {
                BrojKorisnika = totalUsers,
                BrojPoslova = totalJobs,
                BrojKlijenata = totalClients,
                BrojRadnika = totalWorkers,
                BrojZavrsenihPoslova = finishedJobs

            };

            return View("~/Views/Admin/Index.cshtml", model);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteDocument(string fileName)
        {
            var path = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents", fileName);
            if (System.IO.File.Exists(path))
            {
                System.IO.File.Delete(path);
            }

            // Obrisi zapis o odobrenom dokumentu iz baze (ako postoji)
            var odobreniDokument = _context.OdobreniDokumenti.FirstOrDefault(d => d.FileName == fileName);
            if (odobreniDokument != null)
            {
                _context.OdobreniDokumenti.Remove(odobreniDokument);
                _context.SaveChanges();
            }


            return RedirectToAction("Documents");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult ApproveDocument(string fileName)
        {
            var userId = fileName.Split('_')[0];  // pretpostavljam da je userId dio imena fajla

            var korisnik = _userManager.FindByIdAsync(userId).Result;
            if (korisnik != null)
            {
                korisnik.Verified = true; // postavi verifikaciju korisniku
                var result = _userManager.UpdateAsync(korisnik).Result; // update u bazi
            }

            // Dodaj i odobreni dokument u bazu ako želiš
            if (!_context.OdobreniDokumenti.Any(a => a.FileName == fileName))
            {
                _context.OdobreniDokumenti.Add(new OdobreniDokumenti
                {
                    FileName = fileName
                });
                _context.SaveChanges();
            }

            return RedirectToAction("Documents");
        }
    }
}
