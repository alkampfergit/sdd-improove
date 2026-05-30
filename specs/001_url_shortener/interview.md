# Interview — Url Shortener

**Feature**: 001 — Url Shortener
**Date**: 2026-05-22

## Original brief

> simple url shortener feature, user paste a link get a shorten version based on the current domain plus a random string that must be unique

## Q1 — Restate
**Reasoning shown:** Restated as web UI + JSON API on React/Express; lookup returns HTTP redirect (not a preview page).
**Question:** Is the restatement correct, including the two inferences (web feature, redirect on lookup)?
**Answer:** correct

## Q2 — Who / Authentication
**Reasoning shown:** Options: anonymous-only (simplest MVP, abuse risk); authenticated-only (adds auth scope); mixed (anonymous works, optional account for history).
**Question:** Is this feature anonymous-only, authenticated-only, or mixed?
**Answer:** anonymous

## Q3 — Short code format
**Reasoning shown:** Options: ~6–7 char base62 alphanumeric (Bitly-style, ~56B combos at 6 chars); longer 8–10 chars (safer, uglier); readable word slugs (friendlier, more code).
**Question:** What character set and length for the random short code?
**Answer:** Short alphanumeric is good. (Defaulting to 7 characters of [a-zA-Z0-9] unless changed in later questions.)

## Note
**User feedback:** Asked me to use the standard Claude questioning feature (AskUserQuestion tool) rather than plain text. Switching for remaining questions.

## Q4 — Duplicates
**Reasoning shown:** Options: always-new code (simple, multiple links per URL); return existing code (stable links, lookup by long URL); per-session reuse (rarely useful).
**Question:** When the same long URL is shortened twice, return the same code or a new one?
**Answer:** Always new code

## Q5 — Code length
**Reasoning shown:** Options: fixed 7 chars base62 (~3.5T combos); fixed 6 chars base62 (~56B combos); grow on collision (complex, large scale only).
**Question:** Fixed or growing code length?
**Answer:** Fixed 6 chars base62 — supersedes the Q3 default of 7

## Q6 — Invalid input
**Reasoning shown:** Options: reject with error (validate http/https, standard); accept anything (risky, XSS via javascript:); auto-prepend https:// (forgiving).
**Question:** What happens when a user submits an invalid URL?
**Answer:** Reject with error message

## Q7 — Analytics
**Reasoning shown:** Options: out of scope (tight MVP); simple click count; detailed per-visit analytics.
**Question:** Are click analytics part of this feature?
**Answer:** No — out of scope

## Q8 — Unknown code
**Reasoning shown:** Options: 404 with simple message; redirect to homepage (masks broken links); branded 404 landing page.
**Question:** What happens when a visitor hits a short code that doesn't exist?
**Answer:** HTTP 404 with simple message

## Q9 — Lifetime
**Reasoning shown:** Options: never expire (simplest, classic); fixed period (caps growth); user-chosen expiry (more UI).
**Question:** Do short links ever expire?
**Answer:** Never expire

## Q10 — Scale
**Reasoning shown:** Options: small (<100k links, <100 rps); medium (<10M, <1000 rps); generic latency-only SCs.
**Question:** MVP scale target?
**Answer:** Small — < 100k links, < 100 redirects/sec

## Q11 — Result UI
**Reasoning shown:** Options: display + copy button (standard); plain text; display + copy + QR.
**Question:** How is the short URL surfaced to the user after shortening?
**Answer:** Display + copy button

## Technical questions

### TQ1 — SQLite driver vs async principle
**Reasoning shown:** Constitution recommends `better-sqlite3` (sync) but also forbids sync I/O in the request path. Options presented: (a) use better-sqlite3 and amend the async principle via ADR (embedded SQLite calls are sub-ms); (b) use an async driver like `sqlite` (promise wrapper) and override the Stack recommendation via ADR; (c) stop and decide later.
**Question:** How should we resolve the constitution conflict between 'better-sqlite3 recommended' (sync) and 'async DB drivers' (async everywhere)?
**Answer:** Use better-sqlite3 + amend via ADR

### TQ2 — Language
**Reasoning shown:** Options: TypeScript (both backend and frontend) — stronger contracts, matches constitution's `.ts`/`.tsx` test suffix mention; or JavaScript (both) — less ceremony.
**Question:** What language for the backend and frontend?
**Answer:** TypeScript (both)

### TQ3 — Max long URL length (resolves FR-012 NEEDS CLARIFICATION)
**Reasoning shown:** Options: 2048 chars (spec default, historical IE limit, covers ~all real URLs); 8192 chars (more generous); no hard limit (DB-bound only, riskier).
**Question:** Maximum long URL length?
**Answer:** 2048 chars (spec default)
