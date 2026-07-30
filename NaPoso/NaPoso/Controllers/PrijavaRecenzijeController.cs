using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;

namespace NaPoso.Controllers
{
    [Authorize]
    public class PrijavaRecenzijeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;

        public PrijavaRecenzijeController(ApplicationDbContext context, UserManager<Korisnik> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Prijavi(int recenzijaId, string razlog)
        {
            if (string.IsNullOrWhiteSpace(razlog))
            {
                TempData["ErrorMessage"] = "Razlog prijave je obavezan.";
                return RedirectToAction("MojeRecenzije", "Recenzija");
            }

            var userId = _userManager.GetUserId(User);
            var recenzija = await _context.Recenzija.FindAsync(recenzijaId);

            if (recenzija == null)
            {
                return NotFound();
            }

            var prijava = new PrijavaRecenzije
            {
                RecenzijaId = recenzijaId,
                PrijavioKorisnikId = userId,
                Razlog = razlog,
                DatumPrijave = DateTime.UtcNow,
                JeRijeseno = false
            };

            _context.PrijavaRecenzije.Add(prijava);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Recenzija je uspješno prijavljena adminu.";
            return RedirectToAction("MojeRecenzije", "Recenzija");
        }
    }
}
