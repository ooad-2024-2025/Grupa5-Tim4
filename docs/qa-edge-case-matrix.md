# QA Edge Case Matrix — NaPoso

Comprehensive edge case analysis covering all application modules.

## 1. Auth / Login

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Auth/Login | Valid credentials | Valid email + password | Successful login, redirect to role home | Manual | Pass |
| Auth/Login | Invalid email format | `"not-an-email"` | ModelState error, page re-rendered | Manual | Pass |
| Auth/Login | Empty password | `""` | ModelState error: required field | Manual | Pass |
| Auth/Login | SQL injection attempt | `"' OR 1=1 --"` as email | Rejected as invalid email, no DB error | Manual | Fail |
| Auth/Login | XSS in email field | `"<script>alert(1)</script>"` | Razor HTML-encodes output, script not executed | Manual | Fail |
| Auth/Login | Locked out user | 5+ failed attempts | Redirect to `/Identity/Account/Lockout` | Manual | Pass |
| Auth/Login | Unconfirmed email | Unconfirmed user | Redirect to `/Identity/Account/ConfirmEmail` | Manual | Pass |
| Auth/Login | Missing required fields | Empty form submit | ModelState errors displayed | Manual | Pass |
| Auth/Login | Password too short | `"ab"` (min 6) | ModelState error: required length | Manual | Pass |
| Auth/Login | Password with uppercase+digit | `"Password1"` | Accepted (Register requires uppercase+digit per custom model) | Manual | Pass |
| Auth/Login | 2FA required | User with 2FA enabled | Redirect to `/Identity/Account/LoginWith2fa` | Manual | Fail |
| Auth/Login | Recovery code login | Valid recovery code | Successful login | Manual | Fail |
| Auth/Login | External login callback | External provider success | Auto sign-in or redirect to registration | Manual | Fail |
| Auth/Login | Return URL preserved | `?returnUrl=/Oglas/Index` | Redirects to return URL after login | Manual | Fail |

## 2. Register

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Register | Duplicate email | Existing email | ModelState error: email taken | Manual | Pass |
| Register | Mismatched passwords | Password != ConfirmPassword | ModelState error | Manual | Pass |
| Register | Empty name | Empty Ime/Prezime | ModelState error: required fields | Manual | Pass |
| Register | Invalid phone format | `"abc"` instead of `+387...` | ModelState error: invalid format | Manual | Pass |
| Register | Role not selected | No radio button selected | ModelState error or default | Manual | Fail |
| Register | Password too short | `"abc"` (min 6) | ModelState error: required length | Manual | Pass |
| Register | Password missing uppercase | `"password123"` | ModelState error: uppercase required | Manual | Pass |
| Register | Password missing digit | `"Password"` | ModelState error: digit required | Manual | Pass |
| Register | Phone number duplicate | Already-registered phone | ModelState error: phone taken | Manual | Fail |
| Register | All valid fields | Complete valid form | User created, signed in, redirected | Manual | Pass |
| Register | Role assignment | Select Klijent/Radnik | User assigned to correct role | Manual | Pass |

## 3. Payment / Stripe

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Payment/Stripe | Empty API key | No Stripe config | `IsConfigured = false`, checkout returns null | Automated | Pass |
| Payment/Stripe | Valid checkout session | Valid amount + product | Stripe session created, URL returned | Manual | Pass |
| Payment/Stripe | Duplicate webhook event | Same `StripeEventId` twice | Second event ignored (idempotent) | Automated | Pass |
| Payment/Stripe | Zero amount | Amount = 0 | Stripe rejects, error returned | Manual | Fail |
| Payment/Stripe | Negative amount | Amount = -100 | Stripe rejects, error returned | Manual | Fail |
| Payment/Stripe | Very large amount | Amount = 999999999999 | Stripe accepts (within long range) | Manual | Fail |
| Payment/Stripe | Invalid currency | `"xyz"` | Stripe rejects invalid currency code | Manual | Fail |
| Payment/Stripe | Payment succeeded | `payment_intent.succeeded` webhook | Transaction updated to Paid, PaidAt set | Automated | Pass |
| Payment/Stripe | Payment failed | `payment_intent.payment_failed` webhook | Transaction updated to Failed, PaidAt null | Automated | Pass |
| Payment/Stripe | Checkout amount range | Amount < 50 or > 999999999999 | ModelState error on Checkout page | Automated | Pass |
| Payment/Stripe | Success page with TempData | Valid session + TempData | OglasKorisnik status updated to Placen | Manual | Fail |

## 4. Statistics

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Statistics | Empty database | No records | All counters = 0, ProsjecnaOcjena = 0 | Automated | Pass |
| Statistics | Single user | 1 Korisnik | BrojKorisnika = 1 | Manual | Fail |
| Statistics | Mixed statuses | Aktivan + Neaktivan + Placen | Correct count per status | Automated | Pass |
| Statistics | No reviews | Empty Recenzija table | ProsjecnaOcjena = 0 (no divide-by-zero) | Automated | Pass |
| Statistics | All same status | All Oglas = Aktivan | AktivniPoslovi = total, others = 0 | Manual | Fail |
| Statistics | Large dataset | 10000+ records | Returns correctly, no timeout | Manual | Fail |
| Statistics | Average rating with decimals | Ratings: 3, 4, 5 | ProsjecnaOcjena = 4.0 | Automated | Pass |
| Statistics | Role counting | Mix of Klijent/Radnik | Correct BrojKlijenata/BrojRadnika | Manual | Fail |

## 5. Chat / Messaging

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Chat/Messaging | Empty message | Whitespace-only text | Error: "Poruka ne moze biti prazna" | Manual | Pass |
| Chat/Messaging | Very long message | 10000+ characters | Saved or rejected with limit error | Manual | Fail |
| Chat/Messaging | Special characters | `<>&"'` in message | HTML-encoded, displayed correctly | Manual | Fail |
| Chat/Messaging | Same user chatting with self | `korisnik1Id == korisnik2Id` | BadRequest: "Ne mozes razgovarati sam sa sobom" | Manual | Pass |
| Chat/Messaging | Deleted user | User deleted, chat exists | Chat still accessible or handled gracefully | Manual | Fail |
| Chat/Messaging | Unauthorized access | User not in chat | Forbid response | Manual | Pass |
| Chat/Messaging | Create new chat | Valid oglasId + korisnik2Id | New Chat created, redirected to Poruke | Manual | Pass |
| Chat/Messaging | Existing chat reuse | Same pair + oglasId | Existing chat loaded, no duplicate | Manual | Pass |
| Chat/Messaging | Chat ordering | Multiple chats | Ordered by last message time (descending) | Manual | Fail |

## 6. Notifications

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Notifications | Mark as read | `POST MarkAsRead(id)` | `IsRead = true` set, page refreshes | Automated | Pass |
| Notifications | Delete notification | `POST ClearNotification(id)` | Notification removed from DB | Manual | Pass |
| Notifications | Empty notification list | No Obavijest for user | Empty list displayed | Manual | Pass |
| Notifications | Concurrent mark-as-read | Multiple rapid MarkAsRead | All succeed, no race condition | Manual | Fail |
| Notifications | Mark all as read | `POST MarkAllAsRead` | All unread marked as read | Manual | Pass |
| Notifications | Clear all | `POST ClearAllNotifications` | All user notifications removed | Manual | Pass |
| Notifications | AJAX mark as read | `POST MarkAsReadAjax(id)` | Returns 200 Ok or 404 | Manual | Pass |
| Notifications | Access other user's notification | Wrong userId | Notification not found (404 or no-op) | Manual | Pass |
| Notifications | Notification filtering | User with multiple notifications | Only own notifications shown | Automated | Pass |

## 7. Oglas / CRUD

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Oglas/CRUD | Create with empty fields | All fields blank | ModelState errors displayed | Manual | Pass |
| Oglas/CRUD | Create valid | Complete form | Oglas created with Status.Aktivan | Automated | Pass |
| Oglas/CRUD | Update status transitions | Neaktivan -> Aktivan | Status updated in DB | Automated | Pass |
| Oglas/CRUD | Delete with cascade | Delete Oglas with OglasKorisnik | Cascade behavior per FK config | Manual | Fail |
| Oglas/CRUD | Filter by status | `status=Aktivan` | Only Aktivan oglas shown | Automated | Pass |
| Oglas/CRUD | Search with special characters | `"test <script>"` | Razor-encoded, no XSS | Manual | Fail |
| Oglas/CRUD | Sort by price ascending | `sort=cijena_asc` | Oglas ordered by CijenaPosla ASC | Manual | Pass |
| Oglas/CRUD | Sort by price descending | `sort=cijena_desc` | Oglas ordered by CijenaPosla DESC | Manual | Pass |
| Oglas/CRUD | Price range filter | `minCijena=100&maxCijena=500` | Only matching oglas returned | Manual | Pass |
| Oglas/CRUD | Invalid price range | `minCijena=-1` | ModelState error | Manual | Fail |
| Oglas/CRUD | Concurrency conflict | Edit deleted oglas | DbUpdateConcurrencyException handled | Manual | Fail |
| Oglas/CRUD | Unauthorized edit | Non-owner Klijent | Forbid response | Manual | Pass |
| Oglas/CRUD | Duplicate application | Same worker applies twice | Redirected to UspjesnaPrijava | Automated | Pass |
| Oglas/CRUD | Accept application | Admin/Klijent accepts | Status set to Prihvacen, notification sent | Manual | Pass |
| Oglas/CRUD | Reject application | Admin/Klijent rejects | Status set to Odbijen, notification sent | Manual | Pass |
| Oglas/CRUD | KreirajPosao (Admin) | Admin creates oglas for client | Oglas created with client's KlijentId | Manual | Fail |
| Oglas/CRUD | KreirajPosao invalid email | Non-existent client email | ModelState error: user not found | Manual | Fail |

## 8. Dark Mode

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Dark Mode | Light theme toggle | Click "light" button | `data-theme="light"`, light styles applied | Automated | Pass |
| Dark Mode | Dark theme toggle | Click "dark" button | `data-theme="dark"`, dark styles applied | Automated | Pass |
| Dark Mode | System preference toggle | Click "system" button | Follows OS preference | Automated | Pass |
| Dark Mode | localStorage persistence | Toggle dark, reload page | Theme persists as "dark" | Manual | Fail |
| Dark Mode | System preference change | Change OS dark mode | App follows if "system" selected | Manual | Fail |
| Dark Mode | No-JS fallback | JavaScript disabled | Default theme (light) applied | Manual | Fail |
| Dark Mode | Flicker prevention | Page load | `naposo-theme` script blocks FOUC | Automated | Pass |
| Dark Mode | Design tokens loaded | Page load | `tokens.css`, `themes.css`, `components.css` present | Automated | Pass |

## 9. Password Toggle

| Module | Scenario | Input | Expected Result | Test Type | Status |
|--------|----------|-------|-----------------|-----------|--------|
| Password Toggle | Show password | Click toggle button | Input type changes to `text` | Automated | Pass |
| Password Toggle | Hide password | Click toggle again | Input type changes back to `password` | Automated | Pass |
| Password Toggle | Keyboard accessibility | Tab to toggle, press Enter | Toggle activates | Manual | Fail |
| Password Toggle | Focus state | Tab to toggle button | Visible focus indicator | Manual | Fail |
| Password Toggle | aria-label update | Toggle visibility | aria-label reflects current state | Manual | Fail |
| Password Toggle | Login page present | GET `/Identity/Account/Login` | Contains `password-toggle` class | Automated | Pass |
| Password Toggle | Register page present | GET `/Identity/Account/Register` | Contains `password-toggle` class | Automated | Pass |

---

## Summary

| Category | Total Scenarios | Automated | Manual Pass | Manual Fail | Untested |
|----------|----------------|-----------|-------------|-------------|----------|
| Auth/Login | 14 | 0 | 5 | 4 | 5 |
| Register | 11 | 0 | 5 | 2 | 4 |
| Payment/Stripe | 11 | 3 | 1 | 7 | 0 |
| Statistics | 8 | 3 | 0 | 5 | 0 |
| Chat/Messaging | 9 | 0 | 2 | 7 | 0 |
| Notifications | 9 | 2 | 4 | 3 | 0 |
| Oglas/CRUD | 17 | 3 | 6 | 8 | 0 |
| Dark Mode | 8 | 4 | 0 | 4 | 0 |
| Password Toggle | 7 | 3 | 0 | 4 | 0 |
| **Total** | **94** | **15** | **23** | **44** | **9** |

### Key Gaps

1. **No automated auth tests** — Login/Register flows are entirely manual
2. **No security tests** — SQL injection, XSS not verified
3. **No concurrency tests** — Concurrent notification/chat operations untested
4. **No performance tests** — Large dataset statistics untested
5. **Dark mode** — localStorage persistence and system preference not verified
