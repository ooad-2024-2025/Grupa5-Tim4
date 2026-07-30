using Microsoft.EntityFrameworkCore;
using NaPoso.Constants;
using NaPoso.Data;
using NaPoso.Models;
using static NaPoso.Enums.Enums;

namespace NaPoso.Services
{
    public class OglasService : IOglasService
    {
        private readonly ApplicationDbContext _context;

        public OglasService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Oglas?> GetOglasByIdAsync(int id)
        {
            // SOFT DELETE FILTER: Ne vraćamo oglase koji su označeni kao obrisani
            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas == null || oglas.IsDeleted) return null;
            return oglas;
        }

        public async Task<List<Oglas>> GetAllOglasAsync(int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            return await _context.Oglas
                .Where(o => !o.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Oglas>> GetOglasByKlijentIdAsync(string klijentId, int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            return await _context.Oglas
                .Where(o => o.KlijentId == klijentId && !o.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<Oglas>> GetOglasByAutorIdAsync(string autorId, int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            return await _context.Oglas
                .Where(o => (o.KlijentId == autorId || o.RadnikId == autorId) && !o.IsDeleted)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Oglas> CreateOglasAsync(Oglas oglas, string autorId, string autorUloga)
        {
            if (autorUloga == RoleConstants.Klijent)
            {
                oglas.KlijentId = autorId;
                oglas.RadnikId = null;
            }
            else if (autorUloga == RoleConstants.Radnik)
            {
                oglas.RadnikId = autorId;
                oglas.KlijentId = null;
            }
            else
            {
                oglas.KlijentId = autorId;
                oglas.RadnikId = null;
            }

            oglas.Status = Status.Aktivan;

            _context.Add(oglas);
            await _context.SaveChangesAsync();
            return oglas;
        }

        public async Task<Oglas?> UpdateOglasAsync(int id, Oglas input)
        {
            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas == null || oglas.IsDeleted) return null;

            oglas.Opis = input.Opis;
            oglas.Lokacija = input.Lokacija;
            oglas.TipPosla = input.TipPosla;
            oglas.CijenaPosla = input.CijenaPosla;
            oglas.Naslov = input.Naslov;

            await _context.SaveChangesAsync();
            return oglas;
        }

        public async Task<bool> DeleteOglasAsync(int id)
        {
            var oglas = await _context.Oglas.FindAsync(id);
            if (oglas == null) return false;

            // ============================================================
            // SOFT DELETE PATTERN:
            // Ako oglas ima: recenziju (preko Oglas.Recenzija navigacije),
            // plaćanje (PaymentTransaction), ili je već bio povezan sa
            // radnikom (Status != Aktivan ili RadnikId != null)
            // NE BRIŠEMO ga hard (jer bi FOREIGN KEY CONSTRAINT pukao)
            // već ga samo maskiramo kao obrisanog za korisnika
            // (ostaje u bazi zarad istorije i statistike i evidencije)
            // ============================================================
            bool imaPovezanihZapisa =
                (oglas.Status != Status.Aktivan) ||
                (oglas.RadnikId != null) ||
                await _context.Oglas
                    .AnyAsync(o => o.Id == id && o.Recenzija != null) ||           // ima recenziju (navigacija Oglas → Recenzija)
                await _context.PaymentTransactions.AnyAsync(t => t.OglasId == id) ||// ima plaćanje
                await _context.OglasKorisnik.AnyAsync(ok => ok.OglasId == id);     // ima prijava radnika

            if (imaPovezanihZapisa)
            {
                // SOFT DELETE - maskirano brisanje (ostaje u bazi)
                oglas.IsDeleted = true;
                oglas.DeletedAt = DateTime.UtcNow;
                _context.Oglas.Update(oglas);
                await _context.SaveChangesAsync();
                return true;
            }
            else
            {
                // Običan hard delete za potpuno "čiste" oglase koji imaju 0 povezanih zapisa
                _context.Oglas.Remove(oglas);
                await _context.SaveChangesAsync();
                return true;
            }
        }

        public async Task<bool> OglasExistsAsync(int id)
        {
            return await _context.Oglas.AnyAsync(e => e.Id == id && !e.IsDeleted);
        }

        public async Task<List<VerifikovanView>> SearchOglasiAsync(string? search, string? lokacija, string? tipPosla, string? sort, int? minCijena, int? maxCijena, int page = 1, int pageSize = 20)
        {
            var oglasi = from o in _context.Oglas
                         join k in _context.Users on o.KlijentId equals k.Id
                         where o.Status == Status.Aktivan && o.RadnikId == null && !o.IsDeleted
                         select new VerifikovanView
                         {
                             Oglas = o,
                             Verifikovan = k.Verified
                         };

            if (!string.IsNullOrEmpty(search))
                oglasi = oglasi.Where(o => o.Oglas.Naslov.ToLower().Contains(search.ToLower()) || o.Oglas.Opis.ToLower().Contains(search.ToLower()));

            if (!string.IsNullOrEmpty(lokacija))
                oglasi = oglasi.Where(o => o.Oglas.Lokacija.ToLower().Contains(lokacija.ToLower()));

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

            pageSize = Math.Clamp(pageSize, 1, 100);
            return await oglasi
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<List<int>> GetPrijavljeniOglasiAsync(string korisnikId)
        {
            return await _context.OglasKorisnik
                .Where(x => x.KorisnikId == korisnikId && x.OglasId.HasValue)
                .Select(x => x.OglasId!.Value)
                .ToListAsync();
        }

        public async Task<bool> ApplyToOglasAsync(int oglasId, string korisnikId)
        {
            var oglas = await _context.Oglas.FirstOrDefaultAsync(o => o.Id == oglasId && !o.IsDeleted);
            if (oglas == null || oglas.Status != Status.Aktivan || oglas.RadnikId != null)
                return false;

            var postoji = await _context.OglasKorisnik
                .AnyAsync(ok => ok.OglasId == oglasId && ok.KorisnikId == korisnikId);

            if (postoji)
                return false;

            var prijava = new OglasKorisnik
            {
                OglasId = oglasId,
                KorisnikId = korisnikId,
                DatumPrijave = DateTime.UtcNow,
                Status = Status.Aktivan
            };

            _context.OglasKorisnik.Add(prijava);

            if (!string.IsNullOrEmpty(oglas.KlijentId))
            {
                var obavijest = new Obavijest
                {
                    KorisnikId = oglas.KlijentId,
                    Sadrzaj = $"Novi radnik se prijavio na vaš oglas: {oglas.Naslov}",
                    VrijemeSlanja = DateTime.UtcNow,
                    Tip = Obavjestenje.DrugaObavjestenja
                };
                _context.Obavijest.Add(obavijest);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> AcceptApplicationAsync(int prijavaId)
        {
            var prijava = await _context.OglasKorisnik
                .Include(p => p.Oglas)
                .FirstOrDefaultAsync(p => p.Id == prijavaId);

            if (prijava == null || prijava.Oglas == null) return false;

            prijava.Status = Status.Prihvacen;

            var obavijest = new Obavijest
            {
                KorisnikId = prijava.KorisnikId,
                Sadrzaj = $"Vaša prijava na oglas '{prijava.Oglas.Naslov}' je prihvaćena.",
                VrijemeSlanja = DateTime.UtcNow,
                Tip = Obavjestenje.DrugaObavjestenja
            };
            _context.Obavijest.Add(obavijest);

            prijava.Oglas.Status = Status.Prihvacen;

            var ostalePrijave = await _context.OglasKorisnik
                .Where(p => p.OglasId == prijava.OglasId && p.Id != prijavaId && p.Status == Status.Aktivan)
                .ToListAsync();

            foreach (var ostala in ostalePrijave)
            {
                ostala.Status = Status.Neaktivan;
                var obavijestOdbijeno = new Obavijest
                {
                    KorisnikId = ostala.KorisnikId,
                    Sadrzaj = $"Vaša prijava na oglas '{prijava.Oglas.Naslov}' nije odabrana (pozicija je već popunjena).",
                    VrijemeSlanja = DateTime.UtcNow,
                    Tip = Obavjestenje.DrugaObavjestenja
                };
                _context.Obavijest.Add(obavijestOdbijeno);
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> RejectApplicationAsync(int prijavaId, string oglasOwnerId)
        {
            var prijava = await _context.OglasKorisnik
                .Include(p => p.Oglas)
                .FirstOrDefaultAsync(p => p.Id == prijavaId);

            if (prijava == null || prijava.Oglas == null) return false;

            if (prijava.Oglas.KlijentId != oglasOwnerId)
                return false;

            var obavijest = new Obavijest
            {
                KorisnikId = prijava.KorisnikId,
                Sadrzaj = $"Vaša prijava na oglas '{prijava.Oglas.Naslov}' nije odabrana.",
                VrijemeSlanja = DateTime.UtcNow,
                Tip = Obavjestenje.DrugaObavjestenja
            };
            _context.Obavijest.Add(obavijest);
            prijava.Status = Status.Neaktivan;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<OglasKorisnik>> GetApplicantsForOglasAsync(int oglasId, string requestUserId)
        {
            var oglas = await _context.Oglas.FirstOrDefaultAsync(o => o.Id == oglasId && !o.IsDeleted);
            if (oglas == null) return new List<OglasKorisnik>();

            var prijave = await _context.OglasKorisnik
                .Where(ok => ok.OglasId == oglasId)
                .Include(ok => ok.Korisnik)
                .ToListAsync();

            return prijave;
        }

        public async Task<List<OglasKorisnik>> GetRadnikPrijaveAsync(string radnikId)
        {
            return await _context.OglasKorisnik
                .Where(ok => ok.KorisnikId == radnikId)
                .Include(ok => ok.Oglas)
                .OrderByDescending(ok => ok.DatumPrijave)
                .ToListAsync();
        }
    }
}
