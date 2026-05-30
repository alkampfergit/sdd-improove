# Plan: Url Shortener

> Technical design satisfying [spec.md](./spec.md). Cite spec criterion IDs (FR-xxx, SC-xxx) next to design decisions.

## Architecture overview

A single Node.js process serves both the JSON API and the built React frontend, backed by an embedded SQLite database on local disk. The shape follows the constitution's layered architecture exactly: an Express route layer accepts requests and renders responses, a service layer holds validation and code-generation logic, and a repository layer is the only thing that touches `better-sqlite3`. Nothing crosses layers.

```
React (Vite build)  ──HTTP──▶  Express
                                  │
                          routes/links.ts   ──▶  services/links.ts  ──▶  repositories/links.ts  ──▶  links.db (SQLite)
                          (POST /api/links,                                        ▲
                           GET /:code)                                       better-sqlite3 (sync)
```

The two endpoints are deliberately asymmetric: `POST /api/links` is JSON-in/JSON-out and lives under `/api`; the redirect endpoint `GET /:code` lives at the root so the produced short URL — `<PUBLIC_ORIGIN>/<code>` — works without an extra path segment. The frontend is a single page that POSTs to the API and renders the returned `short_url` with a copy button.

Persistence is one table (`links`). Code generation uses `crypto.randomInt` against a 62-character alphabet to produce a fixed 6-character code; collision is detected via the `short_code` PRIMARY KEY constraint and resolved by retrying inside the service. The whole feature runs on one machine; no background jobs, no caches, no outbound services.

## Constitution check

Deviation resolved via [ADR-0001](../../.specify/memory/adrs/0001-embedded-sqlite-exempt-from-async-principle.md) — embedded SQLite via `better-sqlite3` is now an explicit exemption to the "async DB drivers" half of the async-everywhere principle. No other conflicts with the constitution as amended.

## ADRs referenced

- [ADR-0001](../../.specify/memory/adrs/0001-embedded-sqlite-exempt-from-async-principle.md) — Embedded SQLite calls exempt from the async-everywhere principle.

## Architectural changes

This is the project's first feature, so every module listed under [Module layout](#module-layout) is technically new — but each one instantiates a pattern the constitution already prescribes (layered architecture, SQLite persistence, Express routes, React frontend). None introduces a class of behavior the project hasn't seen before:

- New `links` table on the SQLite DB the constitution already names. Within-pattern data-model addition.
- New repository / service / route modules on the layered architecture the constitution already mandates.
- New endpoints on the established Express HTTP surface.

No new trust boundaries (anonymous-only per spec Q2), no background jobs, no outbound services, no auth surface, no scheduled work. All Bucket-2 (within-architecture) changes. No Bucket-1 items beyond ADR-0001, which is already captured above.

## Module layout

Backend lives under `backend/`, frontend under `frontend/`. Test files sit adjacent to the code they cover with the `.test.ts` / `.spec.ts` suffix per the constitution's testing standards.

| File | Exports | Depends on |
|------|---------|------------|
| `backend/src/server.ts` | (entry) | `./app`, `./config` |
| `backend/src/app.ts` | `createApp(deps)` | `express`, `./routes/links`, `./middleware/errors` |
| `backend/src/config.ts` | `loadConfig()` | `process.env` |
| `backend/src/routes/links.ts` | `linksRouter(service)` | `express`, `../services/links` |
| `backend/src/services/links.ts` | `createLinksService(repo, config)` with `shorten(url)` and `resolve(code)` | `../repositories/links`, `../lib/code`, `../lib/validate` |
| `backend/src/repositories/links.ts` | `createLinksRepo(db)` with `insert(code, url)` and `findByCode(code)` | `better-sqlite3` |
| `backend/src/lib/db.ts` | `openDb(path)`, `migrate(db)` | `better-sqlite3`, `node:fs` |
| `backend/src/lib/code.ts` | `generateCode()` (6-char base62) | `node:crypto` |
| `backend/src/lib/validate.ts` | `validateLongUrl(input, maxLen)` → `{ ok, url }` \| `{ ok: false, error }` | `node:url` |
| `backend/src/middleware/errors.ts` | error handler that logs and renders JSON | `express` |
| `frontend/index.html`, `frontend/src/main.tsx`, `frontend/src/App.tsx` | React app | `react`, `react-dom` |
| `frontend/src/api.ts` | `shortenUrl(longUrl)` | `fetch` |
| `backend/src/lib/code.test.ts`, `backend/src/lib/validate.test.ts`, `backend/src/services/links.test.ts`, `backend/src/repositories/links.test.ts`, `backend/src/routes/links.test.ts` | Jest + Supertest tests | code under test |
| `backend/__tests__/e2e/shorten.spec.ts` | Playwright happy-path | the running app |

Routes import services; services import repositories; repositories import the driver. No layer reaches past the one below (constitution: layered architecture).

## Data model

One new table on a new SQLite database file.

```sql
CREATE TABLE links (
  short_code TEXT PRIMARY KEY,           -- 6 chars, [a-zA-Z0-9] (FR-004, FR-005)
  long_url   TEXT NOT NULL,              -- stored byte-for-byte as accepted (FR-009, edge case: unicode)
  created_at TEXT NOT NULL DEFAULT (datetime('now'))   -- ISO-8601 UTC (key entity: created_at)
);
```

`short_code` as `PRIMARY KEY` is the uniqueness guarantee (FR-005) and the collision-detection mechanism — a duplicate insert raises `SQLITE_CONSTRAINT_PRIMARYKEY`, which the service catches and retries. `long_url` is `TEXT` with no length constraint at the DB layer; the service rejects anything over `MAX_URL_LEN` before insert (FR-012). No `updated_at` because links are immutable (FR-011).

## API contract

### POST /api/links — shorten a URL

- Request: `Content-Type: application/json`, body `{ "url": "<long URL>" }`.
- 201 Created → `{ "short_url": "<PUBLIC_ORIGIN>/<code>" }` where `<code>` matches `^[a-zA-Z0-9]{6}$` (FR-004, FR-007, satisfies A1.1).
- 400 Bad Request → `{ "error": "<human-readable reason>" }` for empty input, missing/wrong scheme, malformed URL, or length > `MAX_URL_LEN` (FR-002, FR-003, FR-012, satisfies A2.1–A2.3 and SC-005).
- 500 Internal Server Error → `{ "error": "could not generate a unique code" }` only if the collision-retry cap is exhausted (see Algorithms). Logged loudly per the "fail loudly" principle (FR-014).

Each request generates a brand-new code even for previously-seen URLs — no dedup lookup by `long_url` (FR-008, satisfies A1.3).

### GET /:code — follow a short link

- `<code>` must match `^[a-zA-Z0-9]{6}$`. The route's path regex enforces this so requests with a wrong character set or length never reach the repository (edge case: bad shape returns 404 without a lookup).
- 301 Moved Permanently → `Location: <stored long_url>` (FR-009, satisfies A1.2). 301 is chosen over 302 because links never expire (FR-011, spec Q9) — permanent is the honest semantic and lets clients/proxies cache safely.
- 404 Not Found → plain text `"not found"` for any code that doesn't exist (FR-010, satisfies A8 / spec Q8).

`/:code` is registered last in the route table so it does not shadow `/api/...`, the static frontend assets, or any future system routes.

## Algorithms / key decisions

**Random code generation.** Use `node:crypto.randomInt(0, 62)` six times against the alphabet `0-9A-Za-z` to produce a 6-character base62 string. The alphabet has 62 symbols → `62^6 ≈ 5.68 × 10^10` codes. At 100,000 stored links (the SC-003 ceiling) the prior probability of any single new code colliding is ~1.76 × 10⁻⁶, which sits well below the SC-004 budget of 1 × 10⁻⁵. `randomInt` is uniform over `[0, 62)` (no modulo bias). Satisfies FR-004, SC-004.

**Collision handling.** The service attempts `repo.insert(code, url)`; on `SQLITE_CONSTRAINT_PRIMARYKEY` it generates a new code and retries. Retry cap is 5 — at 100k links the probability of 5 consecutive collisions is `(1.76e-6)^5 ≈ 10⁻²⁹`, so hitting the cap effectively never happens in normal operation; if it does, the service throws and the route returns 500 with a logged error (FR-005, FR-014, "fail loudly"). Detecting collision at insert time (rather than `SELECT` first) makes the operation atomic — important for the concurrent-shorten edge case in the spec, where two requests could otherwise both see "code free" and race.

**URL validation.** Built on the platform `URL` constructor in `node:url`: throw → reject; `protocol !== 'http:' && protocol !== 'https:'` → reject; empty `hostname` → reject; raw byte length > `MAX_URL_LEN` (default 2048, per TQ3) → reject. No regex. The `URL` constructor handles unicode hostnames, percent-encoding, and query/fragment preservation correctly, satisfying the spec's "preserve byte-for-byte" edge case (FR-002, FR-012).

**Sync DB calls in the request path.** Repository functions are synchronous (`better-sqlite3`'s API is synchronous); services and routes call them without `await`. This is permitted by [ADR-0001](../../.specify/memory/adrs/0001-embedded-sqlite-exempt-from-async-principle.md). All other I/O (fs, outbound HTTP if any future feature adds it) remains async per the constitution.

**Redirect status.** 301 over 302 — chosen because links never expire (spec Q9, FR-011) and browsers/proxies may cache 301s indefinitely, which is exactly the desired behavior for a permanent shortener. Supports SC-002.

**Frontend copy button.** Use the standard `navigator.clipboard.writeText(short_url)` API. Confirmation is implemented by toggling the button label to "Copied!" for ~1.5 s via a `useState` + `setTimeout` (FR-013, satisfies A3.1). The Clipboard API requires HTTPS or `localhost` — flagged under Deployment.



## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `express` | ^4.19.0 | HTTP framework (constitution: Stack) |
| `better-sqlite3` | ^11.0.0 | SQLite driver (constitution: Stack, ADR-0001) |
| `react` | ^18.3.0 | Frontend (constitution: Stack) |
| `react-dom` | ^18.3.0 | Frontend |
| `vite` | ^5.4.0 | Frontend build + dev server |
| `@vitejs/plugin-react` | ^4.3.0 | Vite React plugin |
| `typescript` | ^5.5.0 | Both backend and frontend (TQ2) |
| `ts-node` | ^10.9.0 | Run TS in dev |
| `jest` | ^29.7.0 | Backend unit + integration tests (constitution: Testing) |
| `ts-jest` | ^29.2.0 | TS support for Jest |
| `supertest` | ^7.0.0 | API integration tests (constitution: Testing) |
| `@playwright/test` | ^1.46.0 | E2E (constitution: Testing) |
| `@types/express`, `@types/node`, `@types/jest`, `@types/supertest`, `@types/better-sqlite3`, `@types/react`, `@types/react-dom` | latest | Type defs |

No URL validation library, no UUID library, no logger framework — Node built-ins (`URL`, `crypto`, `console`) cover this feature. Adding any of those would violate "No speculative abstractions" (constitution: Forbidden).

## Deployment plan

Single Node process. The Express app serves the API at `/api/...`, the redirect at `/:code`, and the Vite-built frontend assets from `frontend/dist/` at `/` (built artifacts only — Vite dev server is only used in development).

Environment variables (constitution: secrets out of code; "Secrets out of code" applies even though none of these are secret — the principle's "environment-specific values" clause covers all of these):

| Var | Default | Purpose |
|-----|---------|---------|
| `PORT` | `3000` | HTTP listen port |
| `DB_PATH` | `./data/links.db` | SQLite file location |
| `PUBLIC_ORIGIN` | `http://localhost:3000` | Used to build `<short_url>` in API responses (FR-007). Must match what users see in the browser. |
| `MAX_URL_LEN` | `2048` | Long-URL byte limit (FR-012, TQ3) |

Build: `npm run build` (TS compile for backend, `vite build` for frontend). Run: `npm start`. Dev: `npm run dev` uses `ts-node` to run `backend/src/server.ts` directly without a pre-compile step, for fast iteration. Migrations: `lib/db.ts#migrate(db)` runs the `CREATE TABLE IF NOT EXISTS` on boot — idempotent, no separate migration tool needed at this scale.

Operational notes:
- `better-sqlite3` is a native module; the deploy host needs a C++ toolchain available at install time (or use prebuilt binaries via `npm install`'s default behavior on supported platforms).
- The DB file's parent directory must exist and be writable by the process. `lib/db.ts#openDb` creates it via `fs.mkdirSync(..., { recursive: true })` at boot.
- The Clipboard API used by the copy button requires either HTTPS or `localhost`. Local dev on `localhost` is fine; any non-localhost deployment must terminate TLS in front of the process (FR-013).

## Risks and mitigations

| ID | Risk | Mitigation |
|----|------|------------|
| R1 | Code collisions exceed the SC-004 budget at scale | 62⁶ ≈ 5.68×10¹⁰ vs. SC-003 ceiling of 10⁵ rows ⇒ prior collision rate ~1.76×10⁻⁶, well under the 10⁻⁵ budget. Retry caps at 5 with loud failure beyond. |
| R2 | Open-redirect abuse — service redirects any URL submitted | Partial: scheme is locked to `http`/`https` (FR-002) so `javascript:` and `data:` are blocked. The feature itself does not validate the destination's safety — accepted as a known cost of being a general-purpose shortener. Out-of-scope work would add a blocklist or abuse reports. |
| R3 | Concurrent shortens race on the same generated code | Insert-then-catch-constraint pattern is atomic at the SQLite layer; both racers cannot persist the same code. Edge case from spec covered. |
| R4 | `better-sqlite3` blocks the event loop on a slow query | Mitigated structurally — embedded local file ops complete in sub-ms (ADR-0001's premise). Revisit if any query grows beyond a few ms (ADR-0001 follow-up). |
| R5 | Native-module install fails on deploy host without a toolchain | Document the requirement in repository README during implementation; rely on `better-sqlite3` prebuilt binaries for supported Node + OS combos. |

## Out of scope

- **Click analytics.** Spec Q7 — explicitly out.
- **Link expiry / TTL.** Spec Q9 — never expire.
- **Authenticated user accounts and per-user link history.** Spec Q2 — anonymous-only.
- **Custom slugs / vanity codes.** Not in spec; would change the uniqueness model.
- **QR codes.** Spec Q11 — copy button only.
- **Rate limiting and abuse controls.** Not in the FRs; risk R2 acknowledges this.
- **Admin tools for listing / deleting links.** FR-011 forbids modification; no admin surface required.
- **Horizontal scaling, replication, multi-instance.** Constitution: single-instance SQLite; revisit via ADR if scale grows.
