# NaPoso Design System

## Brand Identity
- **Name:** NaPoso (Na Pos'o!)
- **Style:** Editorial + Product — clean, atypical, professional
- **No galaxy/space themes, no generic Bootstrap defaults**

## Color Palette

### Light Mode
| Token | Value | Usage |
|-------|-------|-------|
| --color-bg | #f8f9fc | Page background |
| --color-surface | #ffffff | Card/panel background |
| --color-surface-hover | #f1f3f9 | Hover state |
| --color-border | #e2e5ee | Default borders |
| --color-text | #1a1d29 | Primary text |
| --color-text-secondary | #5a6078 | Secondary text |
| --color-text-muted | #8b90a5 | Muted/caption text |
| --color-accent | #5b5fc7 | Primary brand (Indigo) |
| --color-danger | #e5484d | Error/delete actions |
| --color-success | #30a46c | Success states |
| --color-warning | #e5a100 | Warning states |
| --color-coral | #e8604c | Accent alternative |

### Dark Mode
| Token | Value |
|-------|-------|
| --color-bg | #111318 |
| --color-surface | #1a1d27 |
| --color-text | #e8e9ed |
| --color-accent | #7b7fd7 |
| --color-danger | #ff6369 |
| --color-success | #3dd68c |

## Typography
- **Font:** Inter (Google Fonts) + system fallback
- **Scale:** 0.75rem → 2.5rem
- **Weights:** 400 (regular), 500 (medium), 600 (semibold), 700 (bold)

## Spacing Scale
4px, 8px, 12px, 16px, 20px, 24px, 32px, 40px, 48px, 64px

## Border Radius
sm: 6px, md: 10px, lg: 14px, xl: 20px, full: 9999px

## Shadows
xs through xl + focus variants for interactive elements

## Transitions
- fast: 120ms ease (buttons, toggles)
- base: 180ms cubic-bezier(0.4, 0, 0.2, 1) (cards, panels)
- slow: 300ms ease (page transitions)

## Components
- **Navbar:** Sticky, backdrop-filter blur, custom brand color
- **Buttons:** 5 variants (primary, secondary, danger, ghost, success) + sizes
- **Forms:** Custom styled inputs with focus rings, password toggle
- **Cards:** Border + shadow + hover lift
- **Tables:** Striped rows, uppercase headers, hover highlight
- **Alerts:** Left-colored border variants
- **Auth forms:** Centered card layout with icon header
- **Chat:** Bubble layout with mine/other styling
- **Empty states:** Centered icon + message
- **Theme toggle:** 3-button pill (light/dark/system) with SVG icons
