# Controller Coverage Matrix

## HomeController
| Action | GET | POST | Auth Required | Test |
|--------|-----|------|---------------|------|
| Index | OK | N/A | No | PASS |
| Admin | Redirect | N/A | Admin | PASS |
| Radnik | Redirect | N/A | Radnik | PASS |
| Klijent | Redirect | N/A | Klijent | PASS |
| Error | OK | N/A | No | PASS |

## OglasController
| Action | GET | POST | Auth Required | Test |
|--------|-----|------|---------------|------|
| Index | Redirect | N/A | Admin | PASS |
| Details/99999 | 404 | N/A | Any | PASS |
| Details/null | 404 | N/A | Any | PASS |
| UspjesnaPrijava | OK | N/A | No | PASS |
| PrijavaGreska | OK | N/A | No | PASS |
| Create | Redirect | - | Admin,Klijent | N/A (auth needed) |

## RecenzijaController
| Action | GET | POST | Auth Required | Test |
|--------|-----|------|---------------|------|
| Index | Redirect | N/A | Admin | PASS |
| Details/99999 | 404 | N/A | No | PASS |

## ChatController
| Action | GET | POST | Auth Required | Test |
|--------|-----|------|---------------|------|
| Index | OK | N/A | No | PASS |

## AdminController
| Action | GET | POST | Auth Required | Test |
|--------|-----|------|---------------|------|
| Index | Redirect | N/A | Admin | PASS |
| Documents | Redirect | N/A | Admin | PASS |
