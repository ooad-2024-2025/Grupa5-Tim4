using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NaPoso.Data;
using NaPoso.Models;
using SQLitePCL;
using static NaPoso.Enums.Enums;
using Microsoft.AspNetCore.Identity;


namespace NaPoso.Controllers
{
    public class OglasController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<Korisnik> _userManager;
        public OglasController(UserManager<Korisnik> userManager, ApplicationDbContext context)
        {
            _context = context;
            _userManager = userManager;

        }

        [Authorize(Roles = "Admin")]
        // GET: Oglas
        public async Task<IActionResult> Index()
        {
            return View(await _context.Oglas.ToListAsync());
        }

        // GET: Oglas/Details/5
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglas == null)
            {
                return NotFound();
            }

            return View(oglas);
        }

        // GET: Oglas/Create
        [Authorize(Roles = "Admin,Klijent")]
        public IActionResult Create()
        {
            return View();
        }

        //POST: Oglas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Create([Bind("Opis,Lokacija,TipPosla,CijenaPosla,Naslov")] Oglas oglas)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);  // Ili logiraj negdje
                }
                return View(oglas);
            }
            if (ModelState.IsValid)
            {
                var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (userId == null)
                {
                    // Nema usera, vrati neki error ili redirect na login
                    return Unauthorized();
                }
                oglas.KlijentId = userId;
                oglas.Status = Status.Aktivan;
                oglas.RadnikId = null;

                _context.Add(oglas);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(OglasiKlijenta));
            }
            return View(oglas);
        }

        // GET: Oglas/Edit/5
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas == null)
            {
                return NotFound();
            }
            return View(oglas);
        }

        // POST: Oglas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Opis,Lokacija,TipPosla,CijenaPosla,Naslov,Status")] Oglas oglas)
        {
            if (id != oglas.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var oglasIzBaze = await _context.Oglas.FindAsync(id);
                    if (oglasIzBaze == null)
                    {
                        return NotFound();
                    }

                    // Ažuriraj samo potrebna polja
                    oglasIzBaze.Opis = oglas.Opis;
                    oglasIzBaze.Lokacija = oglas.Lokacija;
                    oglasIzBaze.TipPosla = oglas.TipPosla;
                    oglasIzBaze.CijenaPosla = oglas.CijenaPosla;
                    oglasIzBaze.Naslov = oglas.Naslov;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!OglasExists(oglas.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Edit), new { id = oglas.Id });
            }
            return View(oglas);
        }


        // GET: Oglas/Delete/5
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(m => m.Id == id);
            if (oglas == null)
            {
                return NotFound();
            }

            return View(oglas);
        }

        // POST: Oglas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas != null)
            {
                _context.Oglas.Remove(oglas);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(OglasiKlijenta));
        }

        private bool OglasExists(int id)
        {
            return _context.Oglas.Any(e => e.Id == id);
        }
        
        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrikazOglasa(string search, string lokacija, string tipPosla, string sort, int? minCijena, int? maxCijena)
        {

            var oglasi = from o in _context.Oglas
                        join k in _context.Users on o.KlijentId equals k.Id
                        where o.Status == Status.Aktivan && o.RadnikId == null
                        select new VerifikovanView
                        {
                            Oglas = o,
                            Verifikovan = k.Verified
                        };

            if (!string.IsNullOrEmpty(search))
                oglasi = oglasi.Where(o => o.Oglas.Naslov.Contains(search) || o.Oglas.Opis.Contains(search));

            if (!string.IsNullOrEmpty(lokacija))
                oglasi = oglasi.Where(o => o.Oglas.Lokacija.Contains(lokacija));

            if (!string.IsNullOrEmpty(tipPosla))
                oglasi = oglasi.Where(o => o.Oglas.TipPosla == tipPosla);

            if (minCijena.HasValue)
                oglasi = oglasi.Where(o => o.Oglas.CijenaPosla >= minCijena.Value);

            if (maxCijena.HasValue)
                oglasi = oglasi.Where(o => o.Oglas.CijenaPosla <= maxCijena.Value);

            oglasi = sort switch
            {
                "cijena_asc" => oglasi.OrderBy(o => o.Oglas.CijenaPosla),
                "cijena_desc" => oglasi.OrderByDescending(o => o.Oglas.CijenaPosla),
                _ => oglasi.OrderBy(o => o.Oglas.Naslov)
            };

            return View(await oglasi.ToListAsync());
        }
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> OglasiKlijenta()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var oglasi = await _context.Oglas
                .Where(o => o.KlijentId == userId)
                .ToListAsync();

            return View(oglasi);
        }
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> PrijavljeniRadnici(int oglasId)
        {
            // Prvo dohvatimo oglas i proverimo da li klijent koji gleda je vlasnik oglasa
            var oglas = await _context.Oglas
                .FirstOrDefaultAsync(o => o.Id == oglasId);

            if (oglas == null)
                return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (oglas.KlijentId != userId)
                return Forbid();

            // Dohvati prijave za taj oglas, ukljucujuci informacije o radniku (korisniku)
            var prijave = await _context.OglasKorisnik
                .Where(ok => ok.OglasId == oglasId)
                .Include(ok => ok.Korisnik) // ako imas navigacionu property za korisnika
                .ToListAsync();

            return View(prijave);
        }
        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrijaviRadnikaNaOglas(int oglasId)
        {
            try
            {
                // First check if the job is still available
                var oglas = await _context.Oglas
                    .FirstOrDefaultAsync(o => o.Id == oglasId);

                if (oglas == null || oglas.Status != Status.Aktivan || oglas.RadnikId != null)
                {
                    // Job is not available anymore
                    return RedirectToAction("PrijavaGreska");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                // Provjera da li se već prijavio
                var postoji = await _context.OglasKorisnik
                    .AnyAsync(ok => ok.OglasId == oglasId && ok.KorisnikId == userId);

                if (!postoji)
                {
                    var prijava = new Models.OglasKorisnik
                    {
                        OglasId = oglasId,
                        KorisnikId = userId,
                        DatumPrijave = DateTime.Now,
                        Status = Status.Aktivan
                    };
                    var obavijest = new Obavijest
                    {
                        KorisnikId = oglas.KlijentId, // ID klijenta
                        Sadrzaj = $"Novi radnik se prijavio na vaš oglas: {oglas.Naslov}",
                        VrijemeSlanja = DateTime.Now,
                        Tip = Obavjestenje.DrugaObavjestenja
                    };
                    _context.OglasKorisnik.Add(prijava);
                    _context.Obavijest.Add(obavijest);
                    await _context.SaveChangesAsync();
                }
               
                await _context.SaveChangesAsync();

                return RedirectToAction("UspjesnaPrijava");
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error in PrijaviRadnikaNaOglas: {ex.Message}");
                return RedirectToAction("PrijavaGreska");
            }
        }

        [Authorize(Roles = "Radnik")]
        public async Task<IActionResult> PrijaviSe(int oglasId)
        {
            try
            {
                // First check if the job is still available
                var oglas = await _context.Oglas
                    .FirstOrDefaultAsync(o => o.Id == oglasId);

                if (oglas == null || oglas.Status != Status.Aktivan || oglas.RadnikId != null)
                {
                    // Job is not available anymore
                    return RedirectToAction("PrijavaGreska");
                }

                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                var prijava = new OglasKorisnik
                {
                    KorisnikId = userId,
                    OglasId = oglasId,
                    DatumPrijave = DateTime.Now,
                    Status = Status.Aktivan
                };

                _context.OglasKorisnik.Add(prijava);
                await _context.SaveChangesAsync();

                return RedirectToAction("UspjesnaPrijava");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PrijaviSe: {ex.Message}");
                return RedirectToAction("PrijavaGreska");
            }
        }


        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Prihvati(int id)
        {
            var prijava = await _context.OglasKorisnik
    .Include(p => p.Oglas)
    .FirstOrDefaultAsync(p => p.Id == id);

            if (prijava == null)
                return NotFound();

            prijava.Status = Status.Prihvacen;
            var obavijest = new Obavijest
            {
                KorisnikId = prijava.KorisnikId,
                Sadrzaj = $"Vaša prijava na oglas '{prijava.Oglas.Naslov}' je prihvaćena.",
                VrijemeSlanja = DateTime.Now,
                Tip = Obavjestenje.DrugaObavjestenja
            };
            _context.Obavijest.Add(obavijest);

            await _context.SaveChangesAsync();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava.OglasId }); 
        }
        [Authorize(Roles = "Admin,Klijent")]
        public async Task<IActionResult> Odbij(int id)
        {
            var prijava = await _context.OglasKorisnik
                .Include(p => p.Oglas)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (prijava == null || prijava.Oglas == null)
                return NotFound();

            // Provjera da li trenutni korisnik ima pravo odbiti ovu prijavu
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (prijava.Oglas.KlijentId != userId)
                return Forbid();
            var obavijest = new Obavijest
            {
                KorisnikId = prijava.KorisnikId,
                Sadrzaj = $"Vaša prijava na oglas '{prijava.Oglas.Naslov}' je odbijena.",
                VrijemeSlanja = DateTime.Now,
                Tip = Obavjestenje.DrugaObavjestenja
            };
            _context.Obavijest.Add(obavijest);
            prijava.Status = Status.Odbijen;
            await _context.SaveChangesAsync();

            return RedirectToAction("PrijavljeniRadnici", new { oglasId = prijava.OglasId });
        }

        /*[Authorize(Roles = "Klijent")]
        public async Task<IActionResult> InitiatePayment(int oglasId)
        {
            var oglas = await _context.Oglas.FirstOrDefaultAsync(o => o.Id == oglasId);
            if (oglas == null)
            {
                return NotFound();
            }

            TempData["OglasId"] = oglasId;

            // Issue: RadnikId might be null! Only store if it has a value
            if (!string.IsNullOrEmpty(oglas.RadnikId))
            {
                TempData["RadnikId"] = oglas.RadnikId;
                TempData["Debug_HasRadnik"] = "RadnikId found: " + oglas.RadnikId;
            }
            else
            {
                // Critical error: No worker assigned yet
                TempData["Debug_NoRadnik"] = "No worker (RadnikId) is assigned to this job!";
                // Assign a RadnikId for testing purposes only
                // Remove this in production
                string userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                TempData["RadnikId"] = userId; // Temporary workaround
            }

            long amountInCents = (long)(oglas.CijenaPosla * 100);

            var checkoutUrl = $"/Identity/Payment/Checkout?amount={amountInCents}&productName={Uri.EscapeDataString($"Plaćanje za oglas: {oglas.Naslov}")}";

            return Redirect(checkoutUrl);
        }*/
        [Authorize(Roles = "Klijent")]
        public async Task<IActionResult> InitiatePayment(int oglasId, string radnikId)
        {
            var oglas = await _context.Oglas.FirstOrDefaultAsync(o => o.Id == oglasId);
            if (oglas == null)
            {
                return NotFound();
            }

            TempData["OglasId"] = oglasId;
            TempData["RadnikId"] = radnikId;

            long amountInCents = (long)(oglas.CijenaPosla * 100);

            var checkoutUrl = $"/Identity/Payment/Checkout?amount={amountInCents}&productName={Uri.EscapeDataString($"Plaćanje za oglas: {oglas.Naslov}")}";

            return Redirect(checkoutUrl);
        }

        public IActionResult UspjesnaPrijava()
        {
            return View();
        }

        public IActionResult PrijavaGreska()
        {
            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult KreirajPosao()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> KreirajPosao(AdminOglasView model)
        {
            if (ModelState.IsValid)
            {
                var klijent = await _userManager.FindByEmailAsync(model.KlijentEmail);
                if (klijent == null)
                {
                    ModelState.AddModelError("KlijentEmail", "Klijent s tim emailom ne postoji.");
                    return View(model);
                }

                var oglas = new Oglas
                {
                    Naslov = model.Naslov,
                    Opis = model.Opis,
                    Lokacija = model.Lokacija,
                    TipPosla = model.TipPosla,
                    CijenaPosla = model.CijenaPosla,
                    KlijentId = klijent.Id,
                    Status = Status.Aktivan // ili default status koji koristiš
                };

                _context.Add(oglas);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }
    }
}
