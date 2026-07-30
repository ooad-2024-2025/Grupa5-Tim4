# 🎨 UI Dizajn, Navbar i Logika Isplata (Plan za Agenta)

Ovo je eksplicitni priručnik s gotovim kodom za **dizajn i popravke logike**. 
**Agente, tvoj jedini zadatak je da striktno iskopiraš i primijeniš ove promjene na navedenim fajlovima. Nakon svih promjena pokreni `dotnet build` da potvrdiš ispravnost koda.**

---

## 1. Čišćenje Navbara (Previše linkova za radnika)
**Problem:** Glavni meni (Navbar) je pretrpan kod radnika (Dashboard, Razgovori, Recenzije, Oglasi...).
**Rješenje:** Ostavit ćemo samo "Početna" i "Oglasi" u glavnom meniju, a sve lične opcije prebaciti u padajući meni profila.

**A) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Shared/_Layout.cshtml`
- **Akcija:** Unutar `<ul class="navbar-nav me-auto">` izbriši uslove za `Radnik` i `Klijent` role koji renderuju "Razgovore", "Recenzije" i "Moj Dashboard". 
- Glavni navbar (od linije 63 do kraja `<ul>`) treba da izgleda ZNATNO kraće, samo ovako:
  ```html
  <ul class="navbar-nav me-auto">
      <li class="nav-item">
          <a class="nav-link" asp-area="" asp-controller="Home" asp-action="Index">
              <i class="bi bi-house me-1"></i>Početna
          </a>
      </li>
      <li class="nav-item">
          <a class="nav-link" asp-controller="Oglas" asp-action="Index">
              <i class="bi bi-megaphone me-1"></i>Svi Oglasi
          </a>
      </li>
      
      @if (User.IsInRole("Admin"))
      {
          <li class="nav-item">
              <a class="nav-link text-warning" asp-controller="Admin" asp-action="Index">
                  <i class="bi bi-shield-lock me-1"></i>Admin Panel
              </a>
          </li>
      }
  </ul>
  ```

**B) Fajl za izmjenu:** `NaPoso/NaPoso/Views/Shared/_LoginPartial.cshtml`
- **Akcija:** Radnikove i Klijentove lične linkove prebaci pod "Profil" kao Dropdown meni.
- Zamijeni kompletan `@if (SignInManager.IsSignedIn(User))` blok sa ovim modernim Dropdownom:
  ```html
    @if (SignInManager.IsSignedIn(User))
    {
        <li class="nav-item dropdown">
            <a class="nav-link dropdown-toggle d-flex align-items-center" href="#" id="navbarDropdown" role="button" data-bs-toggle="dropdown" aria-expanded="false">
                <i class="bi bi-person-circle fs-5 me-2"></i> @User.Identity.Name
            </a>
            <ul class="dropdown-menu dropdown-menu-end shadow" aria-labelledby="navbarDropdown">
                <li><h6 class="dropdown-header">Moj Nalog</h6></li>
                @if (User.IsInRole("Radnik"))
                {
                    <li><a class="dropdown-item" asp-controller="Home" asp-action="Radnik"><i class="bi bi-speedometer2 me-2"></i>Moj Dashboard</a></li>
                }
                else if (User.IsInRole("Klijent"))
                {
                    <li><a class="dropdown-item" asp-controller="Home" asp-action="Klijent"><i class="bi bi-speedometer2 me-2"></i>Moj Dashboard</a></li>
                }
                <li><a class="dropdown-item" asp-controller="Chat" asp-action="Index"><i class="bi bi-chat-dots me-2"></i>Razgovori</a></li>
                <li><a class="dropdown-item" asp-controller="Recenzija" asp-action="Index"><i class="bi bi-star me-2"></i>Recenzije</a></li>
                <li><hr class="dropdown-divider"></li>
                <li><a class="dropdown-item" asp-area="Identity" asp-page="/Account/Manage/Index"><i class="bi bi-gear me-2"></i>Postavke Profila</a></li>
                <li>
                    <form method="post" asp-area="Identity" asp-page="/Account/Logout" asp-route-returnUrl="/">
                        <button type="submit" class="dropdown-item text-danger"><i class="bi bi-box-arrow-right me-2"></i>Odjavi se</button>
                    </form>
                </li>
            </ul>
        </li>
    }
  ```

---

## 2. Globalno rješavanje blijedog teksta u svim statistikama
**Fajl za izmjenu:** `NaPoso/NaPoso/wwwroot/css/components.css`
- **Zadatak:** Ne moramo dodavati HTML klase na svaki element ponaosob. Trajno ćemo promijeniti CSS.
- **Akcija:** Pronađi definicije za `.stat-label` i `.stat-detail-item` (oko linije 1170-1180) i zamijeni ih ovim jarkim, uočljivim stilovima:
  ```css
  .stat-label {
    font-size: var(--text-small);
    text-transform: uppercase;
    letter-spacing: 0.05em;
    color: #f8fafc !important; /* Jako svijetla bijela za jak kontrast */
    font-weight: 700 !important;
    margin-bottom: var(--space-4);
    opacity: 0.95;
  }

  .stat-detail-item {
    display: inline-flex;
    align-items: center;
    gap: var(--space-1);
    color: #cbd5e1 !important; /* Svijetlo siva */
    font-weight: 500;
  }
  ```

---

## 3. Popravak "Idi na dashboard" linka na Naslovnici
**Fajl za izmjenu:** `NaPoso/NaPoso/Views/Home/Index.cshtml`
- **Akcija:** Promijeni `else` blok oko linije 21 kako bi dinamički vodio na pravi kontroler ovisno o roli, ZAJEDNO SA ISPRAVNIM ADMIN RUTIRANJEM:
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
            }
  ```

---

## 4. Rješavanje problema isplate novca i statistike (Backend logika)
**Problem:** Novac se tehnički proslijedi kroz Stripe metodu `ReleasePayout`, ali se status oglasa nikada ne promijeni u `Zavrsen`, zbog čega se ne prikazuje u Klijentovom i Radnikovom Dashboardu pod zarađeno/potrošeno!
**Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/StripeConnectController.cs`
- **Akcija:** Pronađi metodu `ReleasePayout`. Pronađi dio gdje se ažurira `transaction.Status = PaymentStatus.Released;` (oko linije 232). Ispod toga postavi Oglas na završen! Zalijepi:
  ```csharp
        // Ažuriraj transakciju
        transaction.Status = PaymentStatus.Released;
        transaction.TransferId = transfer.Id;
        transaction.PlatformFeeAmount = platformFee;
        transaction.WorkerUserId = radnik.Id;
        transaction.UpdatedAt = DateTime.UtcNow;

        // OBAVEZNO DODATI OVO: Status posla se zaključava na Završen nakon uspješne isplate!
        oglas.Status = NaPoso.Enums.Enums.Status.Zavrsen;
        _context.Oglas.Update(oglas);
  ```

Nakon svih promjena, pokreni `dotnet build` da potvrdiš uspješnost!
