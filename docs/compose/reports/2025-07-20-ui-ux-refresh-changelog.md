# NaPoso UI/UX Refresh — Changelog

## A) Summary

1. **Dark mode flicker prevention** — inline `<script>` u `<head>` postavlja temu prije nego što CSS učita, eliminisan flicker
2. **System theme podrška** — tri opcije: Light / Dark / System (prati OS preferenciju)
3. **Theme toggle sa SVG ikonama** — sun/moon/monitor ikonice, persistencija u localStorage
4. **Frosted navbar** — backdrop-filter blur + CSS varijabla `--color-bg-frosted` za light i dark
5. **Uklonjeni svi inline stilovi** iz notification badge-ova u _Layout.cshtml
6. **ResetPassword stranica** — prebačena na custom auth-container dizajn sa password toggle SVG
7. **SetPassword stranica** — prebačena na custom dizajn sa password toggle SVG
8. **ForgotPassword stranica** — prebačena na custom auth-container dizajn
9. **Sve Identity stranice** prevedene na bosanski jezik (Lockout, LoginWith2fa, ExternalLogin, itd.)
10. **Manage stranice** — Email, PersonalData, TwoFactorAuth, ExternalLogins, DeletePersonalData, EnableAuthenticator, Disable2fa, ResetAuthenticator, GenerateRecoveryCodes, ShowRecoveryCodes — sve prevedene i stilizovane
11. **Manage navigacija** — prevedena na bosanski sa ikonicama
12. **CSS tokeni** — dodana `--color-bg-frosted`, `.auth-icon`, `.page-header-danger`, `.page-header-success`, `.text-accent`, `.notification-card`, `.chat-avatar`, `.chat-list-item`, `.stat-value-success`, `.stat-value-coral`
13. **Hardkodirane boje uklonjene** — chat bubble #5b5fc7 zamijenjen sa `var(--color-accent)`, navbar rgba zamijenjen sa tokenom
14. **Disabled button stilovi** — `.btn:disabled` sa opacity i cursor
15. **Focus-visible poboljšanja** — outline + outline-offset na form kontrolama
16. **.visually-hidden utility** za screen readere
17. **.form-hint helper** za tekst ispod polja
18. **Nav-pills stilovi** za Manage sidebar navigaciju
19. **Responsive poboljšanja** — mobilni header, auth kartice, tabele
20. **Chat stranice** — refaktorisane da koriste CSS klase umjesto inline stilova
21. **Delete stranice** — koriste `page-header-danger` klasu
22. **Indeks stranice** — delete dugmad koriste `text-accent` i `text-danger` umjesto inline stilova
23. **Payment stranice** — Checkout, Success, Cancel — očišćeni inline stilovi
24. **Error stranica** — koristi auth-icon klasu
25. **Oglas status badge** — zeleni za Aktivan, plavi za Plaćen
26. **Toast notification** — warning boja koristi CSS varijablu umjesto hardkodirane
27. **Micro-interactions** — `--transition-base` poboljšan na 180ms cubic-bezier
28. **MyNotifications** — koristi CSS klase za kartice obavijesti

## B) Files Changed

| File | Description |
|------|-------------|
| `wwwroot/css/site.css` | Dodani tokeni, utility klase, chat avatars, nav-pills, stat modifiers, notification cards, responsive |
| `wwwroot/js/site.js` | System theme podrška, ispravljena warning toast boja |
| `Views/Shared/_Layout.cshtml` | Flicker prevention, system theme toggle, uklonjeni inline stilovi |
| `Views/Shared/Error.cshtml` | auth-icon klasa, lead klasa |
| `Views/Chat/Details.cshtml` | Refaktorisano na CSS klase |
| `Views/Chat/Index.cshtml` | chat-avatar, chat-list-item klase |
| `Views/Chat/Poruke.cshtml` | Refaktorisano na CSS klase |
| `Views/Oglas/Index.cshtml` | text-danger na delete, text-accent na cijeni |
| `Views/Oglas/Details.cshtml` | text-accent na cijeni |
| `Views/Oglas/Delete.cshtml` | page-header-danger, text-accent |
| `Views/Oglas/PrikazOglasa.cshtml` | text-accent na cijeni |
| `Views/Oglas/OglasiKlijenta.cshtml` | text-danger na delete |
| `Views/Oglas/PrijavaGreska.cshtml` | auth-icon, lead, form-hint |
| `Views/Oglas/UspjesnaPrijava.cshtml` | auth-icon, lead, form-hint |
| `Views/Recenzija/Index.cshtml` | text-danger na delete, text-accent na ocjeni |
| `Views/Recenzija/Details.cshtml` | text-accent |
| `Views/Recenzija/MojeRecenzije.cshtml` | text-accent |
| `Views/Recenzija/Delete.cshtml` | page-header-danger |
| `Views/Obavijest/Delete.cshtml` | page-header-danger |
| `Views/Obavijest/Index.cshtml` | text-danger na delete |
| `Views/ObavijestKorisniku/Delete.cshtml` | page-header-danger |
| `Views/ObavijestKorisniku/Index.cshtml` | text-danger na delete |
| `Views/ObavijestKorisniku/MyNotifications.cshtml` | notification-card, notification-title klase |
| `Views/OglasKorisnik/Delete.cshtml` | page-header-danger |
| `Views/OglasKorisnik/Index.cshtml` | text-danger na delete |
| `Views/Admin/Index.cshtml` | stat-value-success, stat-value-coral |
| `Views/Admin/Documents.cshtml` | card-title, form-hint, card-text |
| All Identity Account pages | Custom auth-container dizajn, bosanski prevod, password toggle SVG |
| All Identity Manage pages | Custom content-card dizajn, bosanski prevod, password toggle SVG |
| Identity Manage/_Layout.cshtml | Bosanski prevod |
| Identity Manage/_ManageNav.cshtml | Bosanski prevod sa ikonicama |

## C) Design Tokens

### Colors (Light)
- `--color-accent: #5b5fc7` (Indigo)
- `--color-danger: #e5484d` (Coral Red)
- `--color-success: #30a46c` (Green)
- `--color-warning: #e5a100` (Amber)
- `--color-coral: #e8604c` (Coral)
- Neutralna skala: `#f8f9fc` → `#ffffff` → `#1a1d29`

### Colors (Dark)
- `--color-accent: #7b7fd7` (svijetliji Indigo)
- Prilagođene nijanse za dark mode kontrast

### Typography
- Font: Inter (Google Fonts) + system fallback
- Scale: 0.75rem → 2.5rem
- Weights: 400/500/600/700

### Spacing
- Scale: 4/8/12/16/20/24/32/40/48/64px

### Radius
- sm: 6px, md: 10px, lg: 14px, xl: 20px, full: 9999px

### Shadows
- xs/sm/md/lg/xl + focus variants

### Transitions
- fast: 120ms ease
- base: 180ms cubic-bezier(0.4, 0, 0.2, 1)
- slow: 300ms ease

## D) Accessibility Notes

- **Focus-visible** — dodan na form kontrole sa outline + outline-offset
- **Fokus na password toggle** — box-shadow varijanta
- **aria-label** na password toggle-ima koji se mijenjaju (Prikaži/Sakrij lozinku)
- **keyboard support** na password toggle-ima (Enter/Space)
- **Visually-hidden** utility za screen readere
- **aria-label** na navbar toggler-u (ispravljena tipka "Ot" → "Otvori")
- **role="radiogroup"** na theme toggle-u sa aria-label
- **Smanjen layout shift** — flicker prevention skripta
- **Reduced motion** — media query poštuje prefers-reduced-motion

## E) Known TODO

1. **Ostali inline stilovi** — nekoliko view-ova ima `style="color: var(--color-accent);"` na ikonicama i `cursor: pointer` na tabelama. Ovi stilovi su funkcionalni ali bi se mogli premjestiti u CSS za potpunu konzistentnost.
2. **Chat Poruke.cshtml** — jQuery scroll handler bi trebao koristiti vanilla JS za konzistentnost sa site.js
3. **Delete personal data** — ima inline style na h1 za danger boju (prihvatljivo za delete stranicu)
4. **Payment Success/Cancel** — imaju inline style za h1 boje (prihvatljivo za status stranice)
5. **Mobile menu** — navbar toggler bi mogao imati bolju animaciju
6. **Loading states** — skeleton klase postoje ali nisu korištene u view-ovima
7. **Print styles** — osnovni su, mogu se proširiti za detaljniji print layout
