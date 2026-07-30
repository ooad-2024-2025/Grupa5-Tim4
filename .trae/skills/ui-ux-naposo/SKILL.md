---
name: ui-ux-naposo
description: Koristi ovaj skill kad god se radi na UI/UX izgledu NaPoso platforme (boje, dugmad, kartice, dashboard, dinamični efekti). Sadrži brand paletu, tipografiju i konkretna pravila za komponente.
---

# NaPoso UI/UX Brand Skill

Pogledaj referentni dokument `UI_UX_VIZUELNI_IDENTITET_UPGRADE.md` u root-u projekta za
kompletnu paletu boja (`--brand-primary`, `--brand-accent`, `--brand-accent-2`), tipografsku
skalu i pravila za dinamične efekte (blob pozadina, animirani brojevi, hover na karticama).

Kad agent radi bilo koju UI izmjenu:
1. Provjeri da li koristi CSS varijable iz `:root` umjesto tvrdo-kodiranih boja.
2. Provjeri da li se `--brand-duotone` gradijent koristi na MAKS. jednom mjestu po stranici.
3. Ne koristi emoji u produkcijskom Razor UI-ju — koristi Bootstrap Icons.
4. Nakon izmjene, pokreni `dotnet build` i potvrdi 0 grešaka.
