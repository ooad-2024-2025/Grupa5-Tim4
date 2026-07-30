# NaPoso UI/UX Changelog

## What Was Improved

1. **Dark mode** — Light / Dark / System tema sa persistencijom u localStorage
2. **Flicker prevention** — inline `<script>` u `<head>` postavlja temu prije CSS učitavanja
3. **Theme toggle** — 3 SVG ikone (sun/monitor/moon) u navbaru sa aria-label podrškom
4. **CSS modularizacija** — monolitni site.css (1346 linija) podijeljen u 5 fajlova: tokens, themes, base, components, utilities
5. **Password reveal/hide** — SVG eye/eye-off ikone na svim password poljima (Login, Register, ChangePassword, ResetPassword, SetPassword, DeletePersonalData)
6. **Hardkodirane boje uklonjene** — chat bubbles, navbar, toast notifications zamijenjeni CSS varijablama
7. **Inline stilovi uklonjeni** — notification badges, delete dugmad, page headers, admin stat kartice
8. **Sve Identity stranice prevedene** na bosanski jezik (33+ stranica)
9. **Responsive poboljšanja** — mobilni header, auth kartice, tabele, manage sidebar
10. **Accessibility** — focus-visible na formama, aria-labels na theme toggle i navbar, visually-hidden utility
11. **Component polish** — nav-pills, clickable table rows, chat avatars, notification cards, stat modifiers
12. **Design tokens** — konzistentna spacing skala (4-64px), radius skala, shadow skala, transitions
13. **Testovi** — 60 testova prolazi (12 integracionih za rute + UI content, 48 postojećih)
14. **QA dokumentacija** — detaljni checklist i report
15. **Cleanup** — dead site.css uklonjen, notification badge konsolidiran, bin/obj uklonjen iz gita
16. **Stripe graceful fallback** — korisnička poruka umjesto crash-a kad API key nedostaje
17. **Profile verification** — jasno prikazuje status verifikacije sa upload formom
18. **Chat alignment** — Pošalji dugme centrirano sa textarea
19. **Test infrastructure** — InMemory database za testove (ne zavisi od PostgreSQL)
20. **.gitignore** — proširen sa build artifacts i IDE direktorijima

## Files Changed

### CSS (modularna struktura)
- `wwwroot/css/tokens.css` — CSS custom properties (light + dark)
- `wwwroot/css/themes.css` — Dark mode stil override-ovi
- `wwwroot/css/base.css` — Reset, tipografija, scrollbar, print
- `wwwroot/css/components.css` — Sve komponente
- `wwwroot/css/utilities.css` — Helper klase
- `wwwroot/css/site.css` — UKLONJEN (bio dead code)

### JavaScript
- `wwwroot/js/site.js` — System theme podrška, ispravljena warning toast boja

### Layout
- `Views/Shared/_Layout.cshtml` — Flicker prevention, system theme toggle, CSS modular, notification badge konsolidiran

### Identity Pages (25+)
- Sve stranice ažurirane sa custom dizajnom, password toggle SVG, bosanski prevod

### Views (25+)
- Sve glavne stranice ažurirane sa CSS klasama umjesto inline stilova

### Tests
- `NaPoso.Tests/UiRouteTests.cs` — 12 integracionih testova
- `NaPoso.Tests/TestWebApplicationFactory.cs` — InMemory database za testove
- `NaPoso.Tests/NaPoso.Tests.csproj` — Microsoft.AspNetCore.Mvc.Testing
- `NaPoso/NaPoso/NaPoso.csproj` — InternalsVisibleTo + public partial class Program

### Documentation
- `docs/ui-qa-checklist.md` — QA checklist
- `docs/ui-qa-report.md` — QA report sa rezultatima
- `docs/ui-changelog.md` — Ovaj fajl
- `docs/design-system.md` — Dokumentacija dizajn sistema

### Configuration
- `.env` — Environment varijable (gitignored)
- `.env.example` — Placeholder vrijednosti
- `.gitignore` — Proširen sa build artifacts
- `.dockerignore` — Isključuje .env
- `docker-compose.yml` — Sve env varijable iz .env
- `appsettings.Development.json` — Template za lokalni development
