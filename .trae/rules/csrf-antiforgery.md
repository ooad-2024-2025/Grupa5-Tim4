# Agent 33: CSRF / Anti-forgery Enforcement

## Goal
Dodati anti-forgery zaštitu na mutating POST akcije.

## Tasks
- ChatController.PosaljiPoruku -> [ValidateAntiForgeryToken]
- Audit svih POST/PUT/DELETE form endpointa
- Dodati token u view forme gdje nedostaje
- Dodati integration/security testove za missing token scenario

## Acceptance
- Mutating endpoints imaju aktivnu anti-forgery zaštitu gdje je relevantno.
