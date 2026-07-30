using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using NaPoso.Constants;
using NaPoso.Helpers;
using NaPoso.Services;
using static NaPoso.Enums.Enums;

namespace NaPoso.Controllers
{
    [Authorize(Roles = RoleConstants.Admin)]
    [ApiVersion("1.0")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;
        private readonly IEmailService _emailService;
        private readonly IStatisticsService _statisticsService;

        public AdminController(
            ApplicationDbContext context,
            UserManager<Korisnik> userManager,
            IEmailService emailService,
            IStatisticsService statisticsService)
        {
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _statisticsService = statisticsService;
        }
        [HttpGet]
        public IActionResult CreateAdmin()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAdmin(CreateAdminViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new Korisnik
            {
                UserName = model.Email,
                Email = model.Email,
                Ime = model.Ime,
                Prezime = model.Prezime,
                Verified = true,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, RoleConstants.Admin);
                TempData["SuccessMessage"] = $"Admin {user.Ime} uspješno kreiran.";
                return RedirectToAction("Index", "Admin");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        public async Task<IActionResult> Documents()
        {
            var documentsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents");
            if (!Directory.Exists(documentsPath))
                Directory.CreateDirectory(documentsPath);

            var files = Directory.GetFiles(documentsPath);

            // Dohvati listu odobrenih dokumenata iz baze
            var approvedFiles = await _context.OdobreniDokumenti.Select(a => a.FileName).ToListAsync();

            var dokumenti = new List<DokumentiKorisnika>();

            var userIds = new List<string>();
            var unapprovedFiles = new List<string>();

            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                if (approvedFiles.Contains(fileName)) continue;

                unapprovedFiles.Add(fileName);
                var userId = fileName.Split('_')[0];
                if (!userIds.Contains(userId))
                {
                    userIds.Add(userId);
                }
            }

            var korisnici = await _context.Users
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id);

            foreach (var fileName in unapprovedFiles)
            {
                var userId = fileName.Split('_')[0];
                korisnici.TryGetValue(userId, out var korisnik);

                dokumenti.Add(new DokumentiKorisnika
                {
                    FileName = fileName,
                    Korisnik = korisnik,
                    DocumentPath = $"/documents/{fileName}"  // putanja za view
                });
            }

            return View(dokumenti);
        }

        public async Task<IActionResult> Index()
        {
            var model = await _statisticsService.GetStatisticsAsync();
            return View("~/Views/Admin/Index.cshtml", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> DeleteDocument(string fileName)
        {
            // Path traversal defense
            var sanitizedFileName = Path.GetFileName(fileName); // strips directory components
            if (sanitizedFileName != fileName || string.IsNullOrWhiteSpace(sanitizedFileName))
                return BadRequest("Invalid file name.");

            // Verify file is within documents directory
            var documentsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents"));
            var fullPath = Path.GetFullPath(Path.Combine(documentsRoot, sanitizedFileName));
            if (!fullPath.StartsWith(documentsRoot, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Access denied.");

            if (System.IO.File.Exists(fullPath))
            {
                System.IO.File.Delete(fullPath);
            }

            // Obrisi zapis o odobrenom dokumentu iz baze (ako postoji)
            var odobreniDokument = await _context.OdobreniDokumenti.FirstOrDefaultAsync(d => d.FileName == sanitizedFileName);
            if (odobreniDokument != null)
            {
                _context.OdobreniDokumenti.Remove(odobreniDokument);
                await _context.SaveChangesAsync();
            }

            // Send rejection email
            var userId = sanitizedFileName.Split('_')[0];
            var korisnik = await _userManager.FindByIdAsync(userId);
            if (korisnik != null)
            {
                await _emailService.SendDocumentRejectionEmail(
                    korisnik.Email,
                    $"{korisnik.Ime} {korisnik.Prezime}"
                );

                // Send in-app notification
                _context.Obavijest.Add(new Obavijest
                {
                    KorisnikId = userId,
                    Sadrzaj = "Vaš zahtjev za verifikaciju je odbijen.",
                    VrijemeSlanja = DateTime.UtcNow,
                    Tip = NaPoso.Enums.Enums.Obavjestenje.Email
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Documents");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = RoleConstants.Admin)]
        public async Task<IActionResult> ApproveDocument(string fileName)
        {
            // Path traversal defense
            var sanitizedFileName = Path.GetFileName(fileName); // strips directory components
            if (sanitizedFileName != fileName || string.IsNullOrWhiteSpace(sanitizedFileName))
                return BadRequest("Invalid file name.");

            // Verify file is within documents directory
            var documentsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents"));
            var fullPath = Path.GetFullPath(Path.Combine(documentsRoot, sanitizedFileName));
            if (!fullPath.StartsWith(documentsRoot, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Access denied.");

            var userId = sanitizedFileName.Split('_')[0];  // pretpostavljam da je userId dio imena fajla

            var korisnik = await _userManager.FindByIdAsync(userId);
            if (korisnik != null)
            {
                korisnik.Verified = true;
                var result = await _userManager.UpdateAsync(korisnik);

                // Send approval email
                await _emailService.SendDocumentApprovalEmail(
                    korisnik.Email,
                    $"{korisnik.Ime} {korisnik.Prezime}"
                );

                // Send in-app notification
                _context.Obavijest.Add(new Obavijest
                {
                    KorisnikId = userId,
                    Sadrzaj = "Vaš zahtjev za verifikaciju je odobren! Vaš profil je sada verifikovan.",
                    VrijemeSlanja = DateTime.UtcNow,
                    Tip = NaPoso.Enums.Enums.Obavjestenje.Email
                });
                await _context.SaveChangesAsync();
            }

            // Dodaj i odobreni dokument u bazu ako želiš
            if (!await _context.OdobreniDokumenti.AnyAsync(a => a.FileName == sanitizedFileName))
            {
                _context.OdobreniDokumenti.Add(new OdobreniDokumenti
                {
                    FileName = sanitizedFileName
                });
                await _context.SaveChangesAsync();
            }

            return RedirectToAction("Documents");
        }

        /// <summary>
        /// Serves a document file with the correct Content-Type header
        /// so browsers can preview PDFs and images inline.
        /// </summary>
        public IActionResult ViewDocument(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return BadRequest("File name is required.");

            // Path traversal defense
            var sanitizedFileName = Path.GetFileName(fileName);
            if (sanitizedFileName != fileName || string.IsNullOrWhiteSpace(sanitizedFileName))
                return BadRequest("Invalid file name.");

            var documentsRoot = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "documents"));
            var fullPath = Path.GetFullPath(Path.Combine(documentsRoot, sanitizedFileName));
            if (!fullPath.StartsWith(documentsRoot, StringComparison.OrdinalIgnoreCase))
                return BadRequest("Access denied.");

            if (!System.IO.File.Exists(fullPath))
                return NotFound();

            var ext = Path.GetExtension(sanitizedFileName);
            var contentType = FileValidationHelper.GetContentType(ext);

            // PhysicalFile with no download name sets Content-Disposition: inline
            return PhysicalFile(fullPath, contentType);
        }
        public async Task<IActionResult> SeedData()
        {
            await _statisticsService.SeedDataAsync();
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> PrijaveRecenzija()
        {
            var prijave = await _context.PrijavaRecenzije
                .Include(p => p.Recenzija)
                .Include(p => p.PrijavioKorisnik)
                .OrderBy(p => p.JeRijeseno)
                .ThenByDescending(p => p.DatumPrijave)
                .ToListAsync();

            var klijentIds = prijave.Where(p => p.Recenzija != null && p.Recenzija.KlijentId != null)
                                    .Select(p => p.Recenzija.KlijentId)
                                    .Distinct()
                                    .ToList();
            var klijenti = await _context.Users.Where(u => klijentIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Email);

            ViewBag.KlijentiEmails = klijenti;

            return View(prijave);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ObrisiRecenziju(int prijavaId, int recenzijaId)
        {
            var recenzija = await _context.Recenzija.FindAsync(recenzijaId);
            if (recenzija != null)
            {
                _context.Recenzija.Remove(recenzija);
            }

            var prijava = await _context.PrijavaRecenzije.FindAsync(prijavaId);
            if (prijava != null)
            {
                prijava.JeRijeseno = true;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(PrijaveRecenzija));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OdbaciPrijavu(int prijavaId)
        {
            var prijava = await _context.PrijavaRecenzije.FindAsync(prijavaId);
            if (prijava != null)
            {
                prijava.JeRijeseno = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(PrijaveRecenzija));
        }

        public async Task<IActionResult> Oglasi(string search, string statusFilter)
        {
            // Admin treba da vidi SVE oglase, uključujući soft-deleted (IsDeleted = true).
            // Zato eksplicitno isključujemo globalni query filter definisan u ApplicationDbContextu.
            var query = _context.Oglas.AsQueryable().IgnoreQueryFilters();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(o => o.Naslov.Contains(search) || o.Opis.Contains(search));
            }

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<NaPoso.Enums.Enums.Status>(statusFilter, out var statusEnum))
            {
                query = query.Where(o => o.Status == statusEnum);
            }

            var oglasi = await query.OrderByDescending(o => o.Id).ToListAsync();
            return View(oglasi);
        }
    }
}