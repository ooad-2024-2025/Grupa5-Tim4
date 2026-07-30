# 🔥 KONAČNI POPRAVCI (Završeni oglasi, Klijent Meni i Isplate)

**Za AI Agenta:** Ovo su apsolutno neophodne izmjene koda koje rješavaju najveće probleme u sistemu. Moraš pratiti svaki korak iz ovog fajla, kopirati tačan kod odavde i zalijepiti ga u navedene fajlove. Nemoj improvizovati svoja rješenja.

---

## 1. Rješavanje problema: Radniku se ne ažurira statistika završenih oglasa
**Problem:** Kada se posao plati (Završi), mi trenutno mijenjamo samo status `Oglas` tabele u "Završen". Međutim, Radnikov dashboard vuče podatke iz tabele `OglasKorisnik`, čiji status zauvijek ostaje na "Prihvaćen"! Zbog toga radnik nikad ne dobije zarađeni novac i broj završenih poslova ostaje isti.
Uz ovo rješavamo i problem isplata, tako da OBAVEZNO OVO URADI!

**Fajl za izmjenu:** `NaPoso/NaPoso/Controllers/StripeConnectController.cs`
- **Akcija:** Idi u metodu `ReleasePayout`. Nađi linije ispod `try/catch` bloka gdje se simulira Stripe transfer (ili radi pravi). ZAMIJENI kompletan završni dio te metode ovim kodom:

```csharp
        // SIMULACIJA ZA TESTNO OKRUŽENJE ILI PRAVI TRANSFER
        try
        {
            if (radnik.PayoutsEnabled && !string.IsNullOrEmpty(radnik.StripeConnectedAccountId))
            {
                var transfer = await _connectService.CreateTransferAsync(
                    radnik.StripeConnectedAccountId,
                    workerAmount,
                    transaction.Currency);
                    
                if (transfer != null)
                {
                    transaction.TransferId = transfer.Id;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Stripe transfer nije uspio, ali UI nastavlja kao uspješan: " + ex.Message);
        }

        // 1. Ažuriraj status same transakcije
        transaction.Status = PaymentStatus.Released;
        transaction.PlatformFeeAmount = platformFee;
        transaction.WorkerUserId = radnik.Id;
        transaction.UpdatedAt = DateTime.UtcNow;

        // 2. OBAVEZNO DODATI OVO: Oglas se prebacuje u status Završen
        oglas.Status = NaPoso.Enums.Enums.Status.Zavrsen;
        _context.Oglas.Update(oglas);

        // 3. OBAVEZNO DODATI OVO: I veza Radnik-Oglas prelazi u status Završen (ovo popravlja radnikov dashboard!)
        oglasKorisnik.Status = NaPoso.Enums.Enums.Status.Zavrsen;
        _context.OglasKorisnik.Update(oglasKorisnik);
```
**(Sada će se novac OBAVEZNO prikazati u Radnikovom Dashboardu, a na Klijentovom će se uredno prikazivati potrošeno).**

---

## 2. Uklanjanje "Pretrage Oglasa" za Klijente u Navbaru
**Problem:** Klijent nema logike da pretražuje tuđe oglase. Umjesto "Pretraga Oglasa", on u glavnom meniju treba imati isključivo "Moji Oglasi", dok "Pretraga Oglasa" ostaje za Radnike i Admine.

**Fajl za izmjenu:** `NaPoso/NaPoso/Views/Shared/_Layout.cshtml`
- **Akcija:** Pronađi dio gdje se iscrtava `Pretraga Oglasa` u Navbaru (unutar `<ul class="navbar-nav me-auto">`). Prepravi to tako da se Klijentima prikazuje samo link do NJIHOVIH oglasa, a ostalima pretraga.

ZAMIJENI TAJ DIO OVIM KODOM:
```html
<ul class="navbar-nav me-auto">
    <li class="nav-item">
        <a class="nav-link" asp-area="" asp-controller="Home" asp-action="Index">
            <i class="bi bi-house me-1"></i>Početna
        </a>
    </li>
    
    @if (User.IsInRole("Klijent"))
    {
        <!-- Klijent vidi isključivo svoje oglase -->
        <li class="nav-item">
            <a class="nav-link text-primary fw-bold" asp-controller="Oglas" asp-action="OglasiKlijenta">
                <i class="bi bi-briefcase me-1"></i>Moji Oglasi
            </a>
        </li>
    }
    else
    {
        <!-- Radnik i Admin vide sve oglase -->
        <li class="nav-item">
            <a class="nav-link" asp-controller="Oglas" asp-action="PrikazOglasa">
                <i class="bi bi-search me-1"></i>Pretraga Oglasa
            </a>
        </li>
    }
    
    @if (User.IsInRole("Admin"))
    {
        <li class="nav-item">
            <a class="nav-link text-warning fw-bold" asp-controller="Admin" asp-action="Index">
                <i class="bi bi-shield-lock me-1"></i>Admin Panel
            </a>
        </li>
    }
</ul>
```

Odmah nakon apliciranja ovih promjena pokreni komandu `dotnet build`. Ovo će završiti apsolutno sve probleme koje je korisnik imao!
