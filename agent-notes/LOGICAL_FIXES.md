# 🧠 LOGIČKI POPRAVCI (Recenzije i Pristup Oglasima)

**Za AI Agenta:** Ovo je detaljan set instrukcija sa gotovim rješenjima za logičke propuste na koje je korisnik naišao. **Sve što trebaš uraditi je da zamijeniš kod na ovim lokacijama.**

---

## 1. Dozvola svima da vide i pretražuju svoje recenzije (Pristup odbijen Fix)
**Problem:** Kontroler za recenzije dozvoljava pristup samo Adminu, zbog čega Klijent i Radnik ne mogu vidjeti recenzije na svom profilu (dobiju 403 Forbidden). Također, svako treba vidjeti SAMO recenzije vezane za sebe.

**Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/RecenzijaController.cs`
- **Akcija:** Idi na liniju gdje se nalazi metoda `Index` (oko linije 31). Zamijeni taj komad koda u potpunosti SA OVIM:

```csharp
        // GET: Recenzija
        [Authorize(Roles = RoleConstants.Admin + "," + RoleConstants.Klijent + "," + RoleConstants.Radnik)]
        public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
        {
            pageSize = Math.Clamp(pageSize, 1, 100);
            
            var query = _context.Recenzija.AsQueryable();
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Filtriraj logično po ulozi!
            if (User.IsInRole(RoleConstants.Klijent))
            {
                // Klijent vidi samo recenzije koje je on kreirao/ostavio
                query = query.Where(r => r.KlijentId == userId);
            }
            else if (User.IsInRole(RoleConstants.Radnik))
            {
                // Radnik vidi samo recenzije koje su klijenti ostavili njemu
                query = query.Where(r => r.RadnikId == userId);
            }
            // Admin vidi sve recenzije (query ostaje nepromijenjen)

            var recenzije = await query
                .OrderByDescending(r => r.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return View(recenzije);
        }
```

---

## 2. Zabrana prijave na oglas Klijentu i Adminu na listi svih oglasa
**Problem:** Kada se Klijent nađe na stranici "Prikaz Oglasa", on vidi dugme "Prijavi se", iako bi samo Radnici trebali moći da se prijave!

**Fajl za izmjenu:** `NaPoso/NaPoso/Views/Oglas/PrikazOglasa.cshtml`
- **Akcija:** Pronađi dio gdje se provjerava da li je korisnik već prijavljen i iscrtava se dugme "Prijavi se" (oko linije 112). Prepravi taj `if/else` blok tako da `else` sadrži provjeru role. Zamijeni stari blok ovim kodom:

```html
                            @if (prijavljeni != null && prijavljeni.Contains(oglas.Oglas.Id))
                            {
                                <div class="text-success fw-semibold" style="font-size: var(--text-caption);">
                                    <i class="bi bi-check-circle-fill me-1"></i>Prijavljeni ste
                                </div>
                            }
                            else if (User.IsInRole("Radnik"))
                            {
                                <a asp-controller="Oglas" asp-action="PrijaviRadnikaNaOglas" asp-route-oglasId="@oglas.Oglas.Id" class="btn btn-primary btn-sm px-4" onclick="event.stopPropagation()">
                                    <i class="bi bi-box-arrow-in-right me-1"></i>Prijavi se
                                </a>
                            }
```

(Sada samo Radnik dobija dugme, a za Klijenta je to prazan prostor, što je mnogo logičnije).

Nakon izmjena oba fajla, pokreni `dotnet build` da potvrdiš ispravnost koda.
