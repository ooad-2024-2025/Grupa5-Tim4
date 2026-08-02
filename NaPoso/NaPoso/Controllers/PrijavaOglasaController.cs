using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize(Roles = RoleConstants.Radnik)]
    public class PrijavaOglasaController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;

        public PrijavaOglasaController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prijavi(int oglasId, string razlog)
        {
            if (string.IsNullOrWhiteSpace(razlog))
            {
                TempData["ErrorMessage"] = "Razlog prijave je obavezan.";
                return RedirectToAction("Details", "Oglas", new { id = oglasId });
            }

            var userId = _userManager.GetUserId(User);
            var oglas = await _context.Oglas.FindAsync(oglasId);

            if (oglas == null)
            {
                return NotFound();
            }

            var prijava = new PrijavaOglasa
            {
                OglasId = oglasId,
                PrijavioKorisnikId = userId ?? string.Empty,
                Razlog = razlog,
                DatumPrijave = DateTime.UtcNow,
                JeRijeseno = false
            };

            _context.PrijavaOglasa.Add(prijava);
            await _context.SaveChangesAsync();

            // Notify all admins about the new report
            var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == NaPoso.Constants.RoleConstants.Admin);
            if (adminRole != null)
            {
                var adminIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == adminRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var adminId in adminIds)
                {
                    _context.Obavijest.Add(new NaPoso.Models.Obavijest
                    {
                        KorisnikId = adminId,
                        Sadrzaj = $"⚠️ Novi oglas prijavljen: '{oglas.Naslov}'. Razlog: {razlog.Substring(0, Math.Min(razlog.Length, 60))}...",
                        VrijemeSlanja = DateTime.UtcNow,
                        Tip = NaPoso.Enums.Enums.Obavjestenje.Email,
                        IsRead = false
                    });
                }
                await _context.SaveChangesAsync();
            }

            TempData["SuccessMessage"] = "Oglas je uspješno prijavljen adminu.";
            return RedirectToAction("Details", "Oglas", new { id = oglasId });
        }
    }
}
