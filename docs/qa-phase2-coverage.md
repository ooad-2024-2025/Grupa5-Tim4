# Phase-2 Coverage Report

## Test Inventory
| Category | Count | Files |
|----------|-------|-------|
| Unit tests | 20 | Unit/StripeServiceTests.cs, Unit/ModelValidationTests.cs, Unit/PaymentTransactionServiceExtendedTests.cs |
| Integration tests | 50 | Integration/SecurityTests.cs, Integration/ControllerCoverageTests.cs, Integration/AuthorizationTests.cs, Integration/EdgeCaseTests.cs, UiRouteTests.cs |
| Existing tests | 47 | ComprehensiveTests.cs, ModelTests.cs, StatisticsServiceTests.cs, PaymentTransactionServiceTests.cs, PaymentTransactionTests.cs |
| **Total** | **117** | |

## Coverage by Module
| Module | Source LOC (est) | Tests | Est. Coverage |
|--------|-----------------|-------|---------------|
| StripeService | 75 | 5 | ~40% |
| StatisticsService | 56 | 3 | ~60% |
| PaymentTransactionService | 44 | 11 | ~90% |
| OglasService (new) | ~120 | 0 (pending) | 0% |
| OglasController | 544→~200 | 8 | ~15% |
| HomeController | 58 | 5 | ~60% |
| RecenzijaController | 302 | 4 | ~10% |
| ChatController | ~80 | 1 | ~10% |
| AdminController | ~150 | 2 | ~10% |
| ApplicationDbContext | 129 | 15 | ~60% |
| Models | 150+ | 14 | ~50% |

## Gap Analysis
- OglasService needs unit tests (Phase-3 priority)
- Controller layer still below 35% target
- Models need validation attribute tests
