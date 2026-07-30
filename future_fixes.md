# 🛠️ MEGA DETALJAN PLAN ISPRAVKI I UI DIZAJNA (Za Agenta)

Ovo je eksplicitni priručnik s gotovim kodom. **Agente, tvoj jedini zadatak je da striktno iskopiraš i primijeniš ove promjene. NE IZMIŠLJAJ svoja rješenja, drži se apsolutno tačnih isječaka navedenih ispod!** Nakon svih promjena pokreni `dotnet build` da potvrdiš ispravnost koda.

---

## 1. Svjetliji tekst u Admin Statistici (UI Dizajn)
**Fajl za izmjenu:** `NaPoso/NaPoso/Views/Admin/Index.cshtml`
- **Zadatak:** Admin statistika ima blijed tekst. Trebamo da bude jarko bijel da "iskače" na tamnoj pozadini za bolji *premium* osjećaj.
- **Akcija:** Pronađi SVAKI `div` sa klasom `stat-label` unutar `stat-card` kartica i dodaj klasu `text-white fw-bold`. 
- Pronađi i klase `stat-detail-item` i dodaj im `text-white`.
- **Zalijepi tačno ovako u svaku karticu (zamijeni samo labele da odgovaraju):**
  ```html
  <div class="stat-label text-white fw-bold">Ukupno korisnika</div>
  <div class="stat-detail">
      <span class="stat-detail-item text-white"><i class="bi bi-person me-1"></i>@Model.BrojKlijenata klijenata</span>
      <!-- itd... -->
  </div>
  ```

## 2. Navigacija i Rješavanje greške "Idi na dashboard" (Routing i RBAC)
**A) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Home/Index.cshtml` (Početna stranica)
- **Zadatak:** Rješavanje `{"error":"An unexpected error occurred."}` prijavljene od strane admina i `Pristup odbijen` grešaka kod klikanja dugmeta "Idi na dashboard". Admin treba ići na `Admin/Index`, Klijent na `Home/Klijent`, Radnik na `Home/Radnik`.
- **Akcija:** Zamijeni `else` blok u formi za autentifikaciju na početnoj stranici (oko linije 21) TAČNO OVIM KODOM:
  ```html
            else
            {
                @if (User.IsInRole("Radnik"))
                {
                    <a asp-controller="Home" asp-action="Radnik" class="btn btn-primary btn-lg shadow-sm hover-lift">
                        <i class="bi bi-speedometer2 me-2"></i>Idi na dashboard
                    </a>
                }
                else if (User.IsInRole("Klijent"))
                {
                    <a asp-controller="Home" asp-action="Klijent" class="btn btn-primary btn-lg shadow-sm hover-lift">
                        <i class="bi bi-speedometer2 me-2"></i>Idi na dashboard
                    </a>
                }
                else if (User.IsInRole("Admin"))
                {
                    <a asp-controller="Admin" asp-action="Index" class="btn btn-primary btn-lg shadow-sm hover-lift">
                        <i class="bi bi-speedometer2 me-2"></i>Idi na dashboard
                    </a>
                }
                <a asp-controller="Oglas" asp-action="Index" class="btn btn-secondary btn-lg shadow-sm hover-lift ms-2">
                    <i class="bi bi-megaphone me-2"></i>Pregled oglasa
                </a>
            }
  ```

**B) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Shared/_Layout.cshtml` (Glavni Navbar)
- **Zadatak:** Korisnik se žali da do Dashboarda mora uraditi više klikova jer ga nema nigdje osim na jednoj specifičnoj stranici. 
- **Akcija:** U glavnom meniju (unutar `<ul class="navbar-nav me-auto">`, tačno iznad `@if (User.IsInRole("Admin"))`) ubaci dinamički link za Klijenta i Radnika:
  ```html
                        @if (User.IsInRole("Klijent"))
                        {
                            <li class="nav-item">
                                <a class="nav-link text-primary fw-bold" asp-controller="Home" asp-action="Klijent">
                                    <i class="bi bi-speedometer2 me-1"></i>Moj Dashboard
                                </a>
                            </li>
                        }
                        @if (User.IsInRole("Radnik"))
                        {
                            <li class="nav-item">
                                <a class="nav-link text-primary fw-bold" asp-controller="Home" asp-action="Radnik">
                                    <i class="bi bi-speedometer2 me-1"></i>Moj Dashboard
                                </a>
                            </li>
                        }
  ```

## 3. Popravak isplate radnicima i promjena statusa (Backend logika)
**Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/StripeConnectController.cs`
- **Zadatak:** Kada Klijent odobri isplatu (metoda `ReleasePayout`), novac se šalje radniku, ali se `Status` Oglasa NE mijenja u `Zavrsen`. Zbog ovoga se iznos potrošenog novca pogrešno računa na dashboardu.
- **Akcija:** Pronađi metodu `ReleasePayout`. Pronađi gdje se ažurira `transaction.Status = PaymentStatus.Released;` (oko linije 232). Odmah ISPOD toga zalijepi ažuriranje oglasa:
  ```csharp
        transaction.Status = PaymentStatus.Released;
        transaction.TransferId = transfer.Id;
        transaction.PlatformFeeAmount = platformFee;
        transaction.WorkerUserId = radnik.Id;
        transaction.UpdatedAt = DateTime.UtcNow;

        // OBAVEZNO DODATI OVO: Status posla se zaključava na Završen nakon isplate radniku!
        oglas.Status = NaPoso.Enums.Enums.Status.Zavrsen;
  ```

## 4. Klijentski Dashboard za praćenje novca (UI i Backend)
**A) Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/HomeController.cs` (Klijent akcija)
- **Akcija:** Napiši upit u metodu `Klijent()` da klijent dobije svoju dashboard statistiku:
  ```csharp
        [Authorize(Roles = RoleConstants.Klijent)]
        public async Task<IActionResult> Klijent()
        {
            var klijentId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var aktivniOglasi = await _context.Oglas.CountAsync(o => o.KlijentId == klijentId && o.Status == NaPoso.Enums.Enums.Status.Aktivan);
            var zavrseniOglasi = await _context.Oglas.CountAsync(o => o.KlijentId == klijentId && o.Status == NaPoso.Enums.Enums.Status.Zavrsen);
            var ukupnoPotroseno = await _context.Oglas
                .Where(o => o.KlijentId == klijentId && o.Status == NaPoso.Enums.Enums.Status.Zavrsen)
                .SumAsync(o => o.CijenaPosla);

            ViewBag.AktivniOglasi = aktivniOglasi;
            ViewBag.ZavrseniOglasi = zavrseniOglasi;
            ViewBag.UkupnoPotroseno = ukupnoPotroseno;

            return View();
        }
  ```

**B) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Home/Klijent.cshtml`
- **Akcija:** Odmah ispod zaglavlja stranice (`<div class="page-header">...</div>`), ugradi tačno ovaj dinamični HTML blok sa karticama za novac:
  ```html
<div class="row g-4 mb-5 animate-fade-in-up">
    <div class="col-md-4">
        <div class="stat-card shadow-sm hover-lift">
            <div class="stat-value text-primary">@ViewBag.AktivniOglasi</div>
            <div class="stat-label text-white fw-bold">Aktivni oglasi</div>
            <div class="stat-detail">
                <span class="stat-detail-item text-white"><i class="bi bi-megaphone me-1"></i>Trenutno u toku</span>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="stat-card shadow-sm hover-lift">
            <div class="stat-value text-success">@ViewBag.ZavrseniOglasi</div>
            <div class="stat-label text-white fw-bold">Završeni oglasi</div>
            <div class="stat-detail">
                <span class="stat-detail-item text-white"><i class="bi bi-check2-circle me-1"></i>Uspješno obavljeni</span>
            </div>
        </div>
    </div>
    <div class="col-md-4">
        <div class="stat-card shadow-sm hover-lift" style="background: linear-gradient(135deg, #1f2937 0%, #111827 100%);">
            <div class="stat-value stat-value-coral">@ViewBag.UkupnoPotroseno KM</div>
            <div class="stat-label text-white fw-bold">Ukupno isplaćeno radnicima</div>
            <div class="stat-detail">
                <span class="stat-detail-item text-white"><i class="bi bi-wallet2 me-1"></i>Plaćeno preko Stripe platforme</span>
            </div>
        </div>
    </div>
</div>
  ```

## 5. Filtriranje i pretraga oglasa za Admina
**A) Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/AdminController.cs`
- **Akcija:** Promijeni metodu `Oglasi` u potpunosti na ovaj kod:
  ```csharp
  public async Task<IActionResult> Oglasi(string search, string statusFilter)
  {
      var query = _context.Oglas.Include(o => o.Klijent).AsQueryable();

      if (!string.IsNullOrEmpty(search))
      {
          query = query.Where(o => o.Naslov.Contains(search) || o.Opis.Contains(search));
      }

      if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<NaPoso.Enums.Enums.Status>(statusFilter, out var statusEnum))
      {
          query = query.Where(o => o.Status == statusEnum);
      }

      var oglasi = await query.OrderByDescending(o => o.DatumObjave).ToListAsync();
      return View(oglasi);
  }
  ```

**B) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Admin/Oglasi.cshtml`
- **Akcija:** Umetni HTML formu za pretragu iznad `<div class="table-responsive">`:
  ```html
  <form method="get" asp-action="Oglasi" class="mb-4 bg-dark p-3 rounded-3 shadow-sm border border-secondary">
      <div class="row g-3">
          <div class="col-md-5">
              <input type="text" name="search" class="form-control" placeholder="Pretraži po naslovu ili opisu..." value="@Context.Request.Query["search"]" />
          </div>
          <div class="col-md-4">
              <select name="statusFilter" class="form-select">
                  <option value="">Svi statusi</option>
                  <option value="Aktivan" selected="@(Context.Request.Query["statusFilter"] == "Aktivan")">Aktivan</option>
                  <option value="Zavrsen" selected="@(Context.Request.Query["statusFilter"] == "Zavrsen")">Završen</option>
              </select>
          </div>
          <div class="col-md-3">
              <button type="submit" class="btn btn-primary w-100 fw-bold"><i class="bi bi-search me-2"></i>Filtriraj listu</button>
          </div>
      </div>
  </form>
  ```

## 6. Popravak boja teksta na "Prihvati / Odbij" dugmadima (Radnici)
**Fajl za izmjenu:** `NaPoso/NaPoso/Views/Oglas/PrijavljeniRadnici.cshtml`
- **Akcija:** Zalijepi tačno ovo u View kod za odobravanje radnika (linije ~47, 48), dodajući jake kontrastne boje i deblji font:
  ```html
  <a asp-action="Prihvati" asp-route-id="@prijava.Id" class="btn btn-success btn-sm flex-grow-1 text-dark fw-bold shadow-sm hover-lift"><i class="bi bi-check-lg me-1"></i>PRIHVATI</a>
  
  <a asp-action="Odbij" asp-route-id="@prijava.Id" class="btn btn-danger btn-sm flex-grow-1 text-white fw-bold shadow-sm hover-lift"><i class="bi bi-x-lg me-1"></i>ODBIJ</a>
  ```
