# Phase-4 Performance Remediation

## Pagination Added
| Endpoint | Before | After |
|----------|--------|-------|
| OglasService.SearchOglasiAsync | Unbounded | page/pageSize with max 100 |
| OglasService.GetAllOglasAsync | Unbounded | page/pageSize with max 100 |
| OglasService.GetOglasByKlijentIdAsync | Unbounded | page/pageSize with max 100 |

## Remaining Performance Gaps
- StatisticsService N+1 on GetRolesAsync (Phase-5 candidate)
- ChatController.Index materializes Poruke for sorting (Phase-5 candidate)
