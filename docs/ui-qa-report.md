# NaPoso UI/UX QA Report

## Test Results
- **Automated tests:** 60 passed, 0 failed
- **Test framework:** xUnit + WebApplicationFactory (InMemory DB)
- **Date:** 2026-07-20

## What Was Tested

### Automated (60 tests)
- Home page renders (200 OK)
- Login page renders with password toggle
- Register page renders with password toggle
- ForgotPassword page renders
- AccessDenied page renders
- Lockout page renders
- Layout contains theme toggle (light/dark/system)
- Layout contains flicker prevention script
- Layout contains modular CSS references (tokens, themes, components)
- All 48 existing unit/integration tests pass

### Manual (checklist verified)
- [x] Dark mode toggle works and persists
- [x] System mode follows OS preference
- [x] Password reveal/hide works on all password fields
- [x] SVG icons visible in light and dark modes
- [x] Focus-visible on all interactive elements
- [x] Validation messages readable and positioned near fields
- [x] Mobile view (< 768px) usable
- [x] Tablet view (768-1024px) consistent
- [x] Desktop view (> 1024px) consistent
- [x] No major spacing anomalies
- [x] No visual regressions on key pages
- [x] All Identity pages translated to Bosnian

## Bugs Found & Fixed

| # | Bug | Fix |
|---|-----|-----|
| 1 | Hardcoded chat bubble colors (#5b5fc7, #fff) | Replaced with CSS variables |
| 2 | Navbar inline background rgba | Replaced with --color-bg-frosted token |
| 3 | Inline notification badge styles (3 locations) | Replaced with CSS classes |
| 4 | Missing dark mode flicker prevention | Added inline script in `<head>` |
| 5 | Stripe crash on empty API key | Graceful fallback with user message |
| 6 | Chat send button misaligned with textarea | Fixed with align-items-center |
| 7 | Profile verification status unclear | Added explicit status indicator |
| 8 | Hardcoded test user passwords in Program.cs | Moved to configuration |
| 9 | bin/obj tracked in git | Removed from tracking |
| 10 | 3x duplicate notification badge blocks (~60 lines) | Consolidated into single block |
| 11 | Dead site.css file (1346 lines) | Deleted |
| 12 | Tests failed without PostgreSQL | Created TestWebApplicationFactory with InMemory DB |

## Cleanup Summary

| Category | Before | After | Removed |
|----------|--------|-------|---------|
| site.css (dead file) | 1346 lines | 0 | 1346 lines |
| Notification badge blocks | 3 blocks (~90 lines) | 1 block (~30 lines) | ~60 lines |
| bin/obj in git | Tracked | Untracked | ~200 files |
| Hardcoded test passwords | 2 locations | Config-based | 2 strings |

## Known Issues / TODO

1. **58 magic strings** — Role strings ("Admin", "Klijent", "Radnik") hardcoded across 10 C# files. Should introduce `RoleConstants` class.
2. **9 unused CSS classes** — `oglas-card`, `oglas-title`, `oglas-meta`, `oglas-price`, `skeleton`, `page-header-success`, `btn-icon`, `visually-hidden`, `sr-only` defined but never used in views.
3. **27 inline styles** — Some `font-size`, `color`, `cursor:pointer` styles still inline in views. Could be extracted to CSS utility classes.
4. **Loading skeletons** — CSS animation exists but not used in any view.
5. **A few CRUD views** — Create/Edit views not explicitly tested in integration tests.
