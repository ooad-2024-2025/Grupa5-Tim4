# UI/UX QA Checklist — NaPoso

## Dark Mode
- [x] Light/Dark/System toggle visible in navbar
- [x] Toggle persists choice in localStorage
- [x] System mode follows OS preference
- [x] No flicker on page load (early theme script works)
- [x] All text readable in dark mode (sufficient contrast)
- [x] All form inputs styled in dark mode
- [x] Tables readable in dark mode
- [x] Modals styled in dark mode
- [x] Alerts readable in dark mode
- [x] Navbar styled in dark mode
- [x] No "white islands" (unstyled blocks) in dark mode

## Password Reveal/Hide
- [x] Login page: eye icon visible, toggles password visibility
- [x] Register page: both password fields have eye toggle
- [x] Change Password page: all 3 fields have eye toggle
- [x] Reset Password page: both fields have eye toggle
- [x] Set Password page: both fields have eye toggle
- [x] Delete Personal Data page: password field has eye toggle
- [x] SVG icons are clear in both light and dark modes
- [x] aria-label changes between "Prikaži lozinku" and "Sakrij lozinku"
- [x] Keyboard accessible (Enter/Space toggle)
- [x] Focus visible on toggle button

## Spacing & Layout
- [x] Consistent vertical rhythm between sections
- [x] Consistent horizontal gaps in grid/flex layouts
- [x] Container max-width consistent across pages
- [x] No random inline styles breaking layout
- [x] Auth forms have consistent padding/margins
- [x] Page headers have consistent spacing
- [x] Content cards have consistent padding

## Components
- [x] Navbar: sticky, backdrop blur, responsive collapse
- [x] Buttons: primary/secondary/danger/ghost all styled
- [x] Buttons: hover/active/focus/disabled states visible
- [x] Form inputs: focus ring visible
- [x] Form validation errors: red border + message near field
- [x] Tables: header styled, rows hover, responsive on mobile
- [x] Cards: border, shadow, hover effect
- [x] Alerts: colored left border, readable text
- [x] Badges: rounded, colored appropriately
- [x] Empty states: icon + message + optional CTA
- [x] Footer: consistent styling

## Responsive
- [x] Mobile (< 768px): navbar collapses to hamburger
- [x] Mobile: tables scroll horizontally
- [x] Mobile: auth cards fit screen
- [x] Mobile: buttons full-width where appropriate
- [x] Tablet (768-1024px): grid adjusts
- [x] Desktop (> 1024px): consistent max-width

## Accessibility
- [x] Focus-visible on all interactive elements
- [x] aria-labels on theme toggle
- [x] aria-label on navbar toggler
- [x] Form labels associated with inputs
- [x] Color is not the only way to convey information
- [x] Keyboard navigation works throughout

## Auth Flow
- [x] Login works with correct credentials
- [x] Login shows validation errors correctly
- [x] Register form submits and creates account
- [x] Register shows validation errors correctly
- [x] Forgot Password flow works
- [x] Reset Password flow works end-to-end
- [x] Logout works and redirects to home
- [x] Access Denied page shows for unauthorized routes

## Regression Check
- [x] Home page loads without errors
- [x] Admin dashboard loads for admin users
- [x] Oglas CRUD works (create, edit, delete, list)
- [x] Chat list and messages display correctly
- [x] Notifications display and mark-as-read works
- [x] Recenzije list displays correctly
- [x] Payment checkout page loads
- [x] Payment success/cancel pages load

## Code Quality
- [x] No dead CSS (site.css removed)
- [x] Notification badge deduplicated
- [x] bin/obj removed from git tracking
- [x] Hardcoded credentials moved to config
- [x] Tests pass with InMemory database
