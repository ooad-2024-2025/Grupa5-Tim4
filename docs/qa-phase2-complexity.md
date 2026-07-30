# Phase-2 Complexity Report

## Before vs After
| File | Before CC | After CC | Change |
|------|-----------|----------|--------|
| OglasController.cs | 69 | ~25 | -64% (after service extraction) |
| AdminController.cs | ~20 | ~18 | -10% (async fix) |
| StatisticsService.cs | 8 | 8 | 0% (async fix only) |
| RecenzijaController.cs | 40 | 40 | 0% (not refactored yet) |

## High-Risk Methods (CC > 10)
| Method | CC | Risk | Recommendation |
|--------|-----|------|----------------|
| RecenzijaController.Create (POST) | 11 | Medium | Extract session validation to service |
| OglasController.PrikazOglasa | ~12 | Medium | Now in OglasService — add tests |
| OglasController.PrijaviRadnikaNaOglas | ~10 | Low | Deduplicated with PrijaviSe |

## Phase-3 Recommendations
- RecenzijaController refactor (CC=40)
- Add OglasService unit tests
- Extract session validation from RecenzijaController
