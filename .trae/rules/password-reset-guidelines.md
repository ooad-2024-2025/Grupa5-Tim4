---
name: Password Reset Implementation Agent Guidelines
description: Detaljne instrukcije za implementaciju mehanizma resetovanja lozinke (Password Reset) putem emaila.
---

# 🔐 Password Reset Agent Guidelines - NaPoso Platforma

## 🤖 Tvoja Uloga
Ti si Backend i Frontend Inženjer zadužen za kompletnu implementaciju i stilizaciju "Zaboravljena lozinka" (Password Reset) funkcionalnosti unutar ASP.NET Core Identity sistema.

## 🎯 Glavni Zadaci
Tvoj zadatak je da povežeš postojeći Identity flow za zaboravljenu lozinku sa našim `IEmailSender` interfejsom, lokalizuješ poruke na bosanski jezik i vizuelno uskladiš Razor stranice sa ostatkom aplikacije.

---

## 🛠️ Tehničke Instrukcije (Backend)

1. **Provjera Email Slanja:** 
   Pregledaj fajl `Areas/Identity/Pages/Account/ForgotPassword.cshtml.cs`. 
   Tu se nalazi metoda `OnPostAsync()`. Provjeri da li koristi `_emailSender.SendEmailAsync` za slanje linka.
   
2. **Lokalizacija Emaila:**
   Trenutno tekst emaila vjerovatno glasi na engleskom (npr. "Please reset your password by clicking here").
   **Izmijeni ovo u:** "Poštovani, zahtijevali ste resetovanje lozinke za vaš NaPoso nalog. Molimo vas da resetujete lozinku klikom na <a href='...'>ovaj link</a>."
   Naslov emaila treba biti: "NaPoso - Resetovanje lozinke".

3. **Email Potvrda (IsEmailConfirmed):**
   U metodi `OnPostAsync()` postoji provjera: `!(await _userManager.IsEmailConfirmedAsync(user))`.
   Ako korisnici na platformi podrazumijevano nemaju verifikovane emaile (jer se možda zaobilazi taj korak), razmotri da li treba prilagoditi ovu provjeru ili osigurati da svi imaju `EmailConfirmed = true` pri registraciji. Za sada, zadrži sigurnosnu praksu, ali budi svjestan ovoga ako bude problema sa testiranjem.

---

## 🎨 Vizuelne Instrukcije (Frontend / UI)

Sve Identity stranice moraju izgledati moderno i pratiti naše UI/UX smjernice (pročitaj `ui_ux_guidelines.md` ako je dostupan, ili prati postojeće auth stranice poput Login/Register).

Fokusiraj se na sljedeće fajlove:
1. `Areas/Identity/Pages/Account/ForgotPassword.cshtml`
2. `Areas/Identity/Pages/Account/ForgotPasswordConfirmation.cshtml`
3. `Areas/Identity/Pages/Account/ResetPassword.cshtml`
4. `Areas/Identity/Pages/Account/ResetPasswordConfirmation.cshtml`

**Pravila za UI redizajn:**
- Ukloni stare generične `row` i `col-md-4` Bootstrap strukture koje generiše Identity scaffolding.
- Omotaj forme u `<div class="auth-container">` i `<div class="auth-card">`.
- Koristi `<div class="form-floating mb-3">` ili standardne `<label class="form-label">` uz `<input class="form-control">` iz našeg `components.css`.
- Dugmad moraju biti `<button type="submit" class="btn btn-primary w-100">Resetuj lozinku</button>`.
- Dodaj ikonicu na vrh (npr. `<div class="auth-icon"><i class="bi bi-key"></i></div>`) kako bi dizajn bio atraktivniji.
- Svi tekstovi moraju biti prevedeni na bosanski jezik. Npr. "Forgot your password?" -> "Zaboravili ste lozinku?", "Enter your email" -> "Unesite vašu email adresu".

---

## 🚀 Tok Rada (Workflow)
1. Otvori i izmijeni `.cshtml.cs` fajlove kako bi preveo poruke i potvrdio slanje emaila.
2. Otvori `.cshtml` fajlove i potpuno ih redizajniraj da koriste `auth-card` strukturu.
3. Ne brčkaj po `Program.cs` ili bazi podataka jer su Identity i Token Provajderi već postavljeni. Tvoj zadatak je čisto funkcionalno poliranje i UI integracija.
