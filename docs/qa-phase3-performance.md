# Phase-3 Performance Baseline

## Methodology
Static analysis of query patterns + code review across controllers, services, and DbContext.

## Key Findings

| Route/Method | Issue | Severity | Recommendation |
|-------------|-------|----------|----------------|
| `AdminController.Documents()` | `Directory.GetFiles()` synchronous file I/O in request path | Medium | Use `Directory.GetFilesAsync()` or move to background task |
| `AdminController.Documents()` | N+1: calls `_userManager.FindByIdAsync()` inside foreach loop over files | High | Batch-load users with `_userManager.GetUsersAsync()` or use a single join query |
| `AdminController.Documents()` | `approvedFiles.Contains(fileName)` uses List.Contains (O(n)) instead of HashSet | Low | Convert to `HashSet<string>` for O(1) lookups |
| `StatisticsService.GetStatisticsAsync()` | Loads ALL users into memory with `ToListAsync()` just to count roles | High | Replace with LINQ `CountAsync` with role filter, or query Identity tables directly |
| `StatisticsService.GetStatisticsAsync()` | N+1: calls `_userManager.GetRolesAsync()` for every user in a loop | High | Use `_context.UserRoles` join with `IdentityRole` for a single query |
| `StatisticsService.GetStatisticsAsync()` | Calls `.CountAsync()` 4 times on `Oglas` table (4 separate round-trips) | Medium | Combine into a single grouped query |
| `OglasController.Index()` | `GetAllOglasAsync()` loads all Oglas records with no pagination | Medium | Add `.Take(50)` default or implement pagination |
| `OglasService.GetAllOglasAsync()` | `ToListAsync()` on entire Oglas table with no limit | Medium | Add `.Take()` or pagination parameter |
| `OglasService.GetOglasByKlijentIdAsync()` | Returns all ads for a client with no limit | Medium | Add pagination |
| `OglasService.SearchOglasiAsync()` | Client-side `ToLower()` on every row prevents index usage | High | Use case-insensitive collation or `EF.Functions.ILike()` for PostgreSQL |
| `OglasService.SearchOglasiAsync()` | No `.Take()` limit on search results | High | Add `.Take(50)` default maximum |
| `OglasKorisnikController.Index()` | Loads all OglasKorisnik records with no pagination | Medium | Add `.Take(50)` |
| `RecenzijaController.Index()` | Loads all Recenzija records with no pagination | Medium | Add `.Take(50)` |
| `RecenzijaController.MojeRecenzije()` | Client-side `FirstOrDefault()` loop instead of dictionary join | Low | Use `ToDictionary()` for O(1) lookups |
| `ChatController.Index()` | Complex `OrderByDescending` with nested `Poruke.Any()/Max()` per chat | High | Denormalize with `LastMessageAt` column on Chat, or use raw SQL |
| `ChatController.Poruke()` | Loads ALL messages for a chat with no pagination | High | Add `.Take(100)` and cursor-based loading |
| `ChatController.Index()` | Includes `Poruke` collection (used only for ordering) causing full materialization | High | Project only the ordering field, not the full collection |
| `ObavijestKorisnikuController.MyNotifications()` | No pagination on notifications | Low | Add `.Take(100)` |
| `PaymentTransactionService.GetByUserIdAsync()` | No pagination on transaction history | Low | Add `.Take(50)` |
| `PaymentTransactionService.GetByOglasIdAsync()` | No pagination on per-oglas transactions | Low | Add `.Take(50)` |

## Database Index Recommendations

| Table | Column(s) | Reason |
|-------|-----------|--------|
| Oglas | `Status` | Filtered by Status in SearchOglasiAsync, StatisticsService |
| Oglas | `KlijentId` | Filtered by KlijentId in GetOglasByKlijentIdAsync |
| Oglas | `RadnikId` | Filtered in SearchOglasiAsync (RadnikId == null check) |
| Oglas | `Naslov` (GIN trigram) | Full-text search on title in SearchOglasiAsync |
| Oglas | `Lokacija` | Filtered in SearchOglasiAsync |
| Oglas | `CijenaPosla` | Sorted/ranged in SearchOglasiAsync |
| Chat | `Korisnik1Id`, `Korisnik2Id` | Queried in ChatController.Index() |
| Chat | `CreatedAt` | Ordered in ChatController.Index() |
| Poruka | `ChatId` | Included when loading chat messages |
| Poruka | `PoslanoAt` | Ordered in Poruke.cshtml |
| Obavijest | `KorisnikId` | Filtered in MyNotifications, Obavijest.Index |
| Obavijest | `IsRead` | Filtered in MarkAllAsRead |
| OglasKorisnik | `OglasId` | Filtered in GetApplicantsForOglasAsync |
| OglasKorisnik | `KorisnikId` | Filtered in GetPrijavljeniOglasiAsync |
| OglasKorisnik | `(OglasId, KorisnikId)` | Composite unique check in ApplyToOglasAsync |
| Recenzija | `RadnikId` | Filtered in MojeRecenzije |
| Recenzija | `KlijentId` | Filtered in MojeRecenzije |
| PaymentTransaction | `OglasId` | Filtered in GetByOglasIdAsync |

## Pagination Gaps

| Endpoint | Current Behavior | Recommendation |
|----------|-----------------|----------------|
| `Oglas/Index` (Admin) | Returns all Oglas records | Add pagination with default 20 per page |
| `OglasKorisnik/Index` (Admin) | Returns all OglasKorisnik records | Add pagination with default 20 per page |
| `Recenzija/Index` (Admin) | Returns all Recenzija records | Add pagination with default 20 per page |
| `Recenzija/MojeRecenzije` (Radnik) | Returns all reviews for radnik | Add pagination with default 20 per page |
| `Obavijest/Index` (Admin) | Returns all notifications for admin | Add pagination with default 20 per page |
| `ObavijestKorisniku/MyNotifications` | Returns all notifications for user | Add `.Take(100)` or pagination |
| `Chat/Index` | Returns all chats for user | Add `.Take(50)` |
| `Chat/Poruke/{id}` | Returns all messages in chat | Add `.Take(100)` with cursor support |
| `Oglas/SearchOglasi` | Returns all matching results | Add `.Take(50)` default maximum |
| `PaymentTransactions` | Returns all transactions | Add pagination with default 20 per page |

## Synchronous I/O in Request Paths

| Location | Operation | Fix |
|----------|-----------|-----|
| `AdminController.Documents()` :33-38 | `Directory.Exists()`, `Directory.GetFiles()` | Wrap in `Task.Run` or use async file enumeration |

## Safe Optimizations Applied

None — this is an analysis-only document. Code changes should be tracked as separate commits.

## Performance Targets

- p95 response time < 200ms for reads
- p95 response time < 500ms for writes
- No unbounded queries in production paths
- Maximum 3 database round-trips per request (where currently 4+ are used in StatisticsService)
- No synchronous file I/O in request paths
