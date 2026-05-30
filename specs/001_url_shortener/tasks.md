# Tasks: Url Shortener

> Atomic units of work, each completable in 30 to 60 minutes. Tests named here match files under the backend/frontend trees with the `.test.ts` / `.spec.ts` suffix per the constitution. Implement in topological order. Spec scenario IDs (`A1.1` etc.) refer to the numbered acceptance scenarios in [spec.md](./spec.md); FR/SC IDs reference its Functional Requirements and Success Criteria.

## T-01 — Backend skeleton: TypeScript, Jest, scripts, folder layout

- **Files touched:** `package.json`, `backend/package.json`, `backend/tsconfig.json`, `backend/jest.config.ts` (with `passWithNoTests: true`), `.gitignore`
- **Acceptance test:** No test file. Acceptance is verified by tooling: `npx tsc --noEmit` exits 0 against an empty `backend/src/` and `npm test` exits 0 (Jest passes with no tests found).
- **Depends on:** (none)
- **Done when:** `npx tsc --noEmit` and `npm test` both exit 0. No FR/SC tied to this task — pure scaffolding.

## T-02 — `lib/db.ts`: open SQLite and run idempotent migration

- **Files touched:** `backend/src/lib/db.ts`, `backend/src/lib/db.test.ts`
- **Acceptance test:** `backend/src/lib/db.test.ts` — opens a temp file DB, calls `migrate(db)`, asserts `PRAGMA table_info(links)` returns columns `short_code TEXT PK`, `long_url TEXT NOT NULL`, `created_at TEXT NOT NULL`; calling `migrate` a second time does not throw.
- **Depends on:** T-01
- **Done when:** The `links` table from [plan §Data model](./plan.md#data-model) is created idempotently and the test passes. Supports FR-006 (durable persistence).

## T-03 — `lib/code.ts`: 6-character base62 generator

- **Files touched:** `backend/src/lib/code.ts`, `backend/src/lib/code.test.ts`
- **Acceptance test:** `backend/src/lib/code.test.ts` — calling `generateCode()` 10,000 times produces only strings matching `^[a-zA-Z0-9]{6}$`; the empirical distribution over the 62 symbols is roughly uniform (chi-square or simple bucket-min/max ratio sanity check).
- **Depends on:** T-01
- **Done when:** Generator satisfies FR-004 (6 chars, base62) and the uniformity check from [plan §Algorithms — Random code generation](./plan.md#algorithms--key-decisions) holds.

## T-04 — `lib/validate.ts`: long-URL validation

- **Files touched:** `backend/src/lib/validate.ts`, `backend/src/lib/validate.test.ts`
- **Acceptance test:** `backend/src/lib/validate.test.ts` — table-driven cases: empty string, `not a url`, `javascript:alert(1)`, `ftp://example.com`, `https://` (no host), one URL longer than `maxLen` → each returns `{ ok: false, error: <non-empty string> }`; valid `http://`/`https://` URLs (including unicode host, query, fragment, percent-encoding) return `{ ok: true, url }`.
- **Depends on:** T-01
- **Done when:** Covers FR-002, FR-003, FR-012 and the inputs enumerated in spec **A2.1**, **A2.2**, **A2.3**.

## T-05 — `repositories/links.ts`: insert + findByCode against real SQLite

- **Files touched:** `backend/src/repositories/links.ts`, `backend/src/repositories/links.test.ts`
- **Acceptance test:** `backend/src/repositories/links.test.ts` — uses a file-based test DB seeded by `migrate()`: `insert("aB3xZ9", "https://example.com")` then `findByCode("aB3xZ9")` returns `{ short_code, long_url, created_at }`; a second `insert("aB3xZ9", …)` throws an error whose code is `SQLITE_CONSTRAINT_PRIMARYKEY`; `findByCode("missing")` returns `undefined`.
- **Depends on:** T-02
- **Done when:** Constitution's "repository owns DB access" rule is satisfied and the constraint-error surface required by the service's collision-retry is exposed. Supports FR-005, FR-006, FR-009.

## T-06 — `services/links.ts`: shorten + resolve with collision retry

- **Files touched:** `backend/src/services/links.ts`, `backend/src/services/links.test.ts`
- **Acceptance test:** `backend/src/services/links.test.ts` — with a real test DB: (a) `shorten("https://example.com/x")` returns `{ short_url: "<origin>/<code>" }` where `<code>` matches `^[a-zA-Z0-9]{6}$`; (b) shortening the same URL twice yields two distinct codes and two rows (A1.3, FR-008); (c) injecting a stub `generateCode` that returns a fixed already-used code first, then a fresh one, proves collision retry inserts the second code (FR-005); (d) exhausting the 5-retry cap throws a loudly-logged error (FR-014); (e) invalid input from `validate` returns `{ ok: false, error }` without touching the repo (SC-005).
- **Depends on:** T-03, T-04, T-05
- **Done when:** All five sub-cases pass and `resolve(code)` returns the stored `long_url` for hits and `undefined` for misses.

## T-07 — `middleware/errors.ts`: loud error handler

- **Files touched:** `backend/src/middleware/errors.ts`, `backend/src/middleware/errors.test.ts`
- **Acceptance test:** `backend/src/middleware/errors.test.ts` — mounts the handler on a throwaway Express app with a route that throws; asserts response is `500` with JSON `{ error: <string> }` and that `console.error` was called with the original error (use a spy).
- **Depends on:** T-01
- **Done when:** FR-014 ("fail loudly") is satisfied — no silent error swallowing per constitution.

## T-08 — `routes/links.ts` + `app.ts`: HTTP surface with Supertest coverage

- **Files touched:** `backend/src/routes/links.ts`, `backend/src/app.ts`, `backend/src/routes/links.test.ts`
- **Acceptance test:** `backend/src/routes/links.test.ts` — Supertest against `createApp({ service, config })` with a real in-memory/temp-file DB:
  - **A1.1:** `POST /api/links { url: "https://example.com/some/long/path?q=1" }` → `201` + body `{ short_url }` matching `^https?://.+/[a-zA-Z0-9]{6}$`.
  - **A1.2:** seed `("aB3xZ9","https://example.com/target")`; `GET /aB3xZ9` → `301` and `Location: https://example.com/target` byte-for-byte (FR-009).
  - **A1.3:** two POSTs of the same URL produce two distinct codes and two rows (FR-008).
  - **A2.1/2.2/2.3:** empty, `javascript:alert(1)`, and `example.com/path` → `400` + `{ error: <non-empty string> }`; assert row count unchanged (FR-003, SC-005).
  - **404:** `GET /zzzzzz`, `GET /aB3xZ-` (bad shape — should never hit repo), `GET /tooshort` → `404` (FR-010).
- **Depends on:** T-06, T-07
- **Done when:** All bullets above pass; route file contains no DB calls (constitution: no DB in routes).

## T-09 — `config.ts` + `server.ts`: env loading and entry point

- **Files touched:** `backend/src/config.ts`, `backend/src/server.ts`, `backend/src/config.test.ts`
- **Acceptance test:** `backend/src/config.test.ts` — `loadConfig()` with no env vars returns the defaults from [plan §Deployment plan](./plan.md#deployment-plan) (`PORT=3000`, `DB_PATH=./data/links.db`, `PUBLIC_ORIGIN=http://localhost:3000`, `MAX_URL_LEN=2048`); each var can be overridden; invalid `MAX_URL_LEN` (non-numeric) throws loudly.
- **Depends on:** T-08
- **Done when:** `npm start` boots the server, creates `DB_PATH`'s parent dir if missing, runs `migrate`, and listens on `PORT`. No FR/SC tied directly — supports operational requirements from the plan.

## T-10 — Frontend skeleton: Vite + React + TypeScript

- **Files touched:** `frontend/package.json`, `frontend/tsconfig.json`, `frontend/vite.config.ts`, `frontend/index.html`, `frontend/src/main.tsx`, `frontend/src/App.tsx` (placeholder showing "Url Shortener")
- **Acceptance test:** No test file. Acceptance is verified by tooling: `npx vite build` succeeds and `npx tsc --noEmit` exits 0; the built `frontend/dist/index.html` exists and references the bundled JS. Parallel-safe with T-01..T-09.
- **Depends on:** (none)
- **Done when:** A static build artifact exists under `frontend/dist/` for T-12 to serve.

## T-11 — Frontend: shorten form, result display, copy button

- **Files touched:** `frontend/src/App.tsx`, `frontend/src/api.ts`, `frontend/src/App.module.css` (or inline styles — implementer's call)
- **Acceptance test:** Covered end-to-end by T-13's Playwright spec (no separate frontend unit test — the constitution does not require Vitest, and Playwright proves the same behavior with less mocking). Manual smoke: with backend running, paste `https://example.com`, submit, see `https://<origin>/<6char>` rendered next to a Copy button.
- **Depends on:** T-10
- **Done when:** `App.tsx` POSTs to `/api/links`, renders `short_url` on 201, renders the `error` field on 400 inline next to the input without clearing the input (A2.1), and the Copy button uses `navigator.clipboard.writeText` with a "Copied!" toggle for ~1.5s (FR-013, **A3.1**).

## T-12 — Express serves the built frontend at `/`

- **Files touched:** `backend/src/app.ts` (extend), `backend/src/routes/links.test.ts` (add one assertion) or new `backend/src/app.test.ts`
- **Acceptance test:** Supertest: `GET /` returns `200` with `Content-Type: text/html` and the body contains the string from `frontend/dist/index.html`. `GET /api/links` and `GET /:code` continue to work — i.e., the static middleware is registered before `/:code` but does not shadow the API or the redirect route.
- **Depends on:** T-09, T-10, T-11
- **Done when:** The single Node process serves both the UI and the API, per [plan §Deployment plan](./plan.md#deployment-plan).

## T-13 — Playwright E2E: shorten → follow → copy

- **Files touched:** `backend/__tests__/e2e/playwright.config.ts`, `backend/__tests__/e2e/shorten.spec.ts`
- **Acceptance test:** `backend/__tests__/e2e/shorten.spec.ts` — Playwright spins up the built app (or `npm start`), then:
  1. Open `/`, type `https://example.com/foo`, click Shorten, expect a short URL of shape `<origin>/[a-zA-Z0-9]{6}` to appear within 5 s (A1.1, SC-006).
  2. Click Copy, assert `navigator.clipboard.readText()` equals the displayed short URL and the button briefly reads "Copied!" (A3.1, FR-013).
  3. `page.goto(short_url)` — assert the final URL is `https://example.com/foo` (A1.2, FR-009).
  4. Submit empty input, expect inline `error` rendered, input retained (A2.1).
- **Depends on:** T-12
- **Done when:** All four steps pass headless. SC-006's "under 10 seconds end-to-end" is asserted by the test's overall timeout being 10s for steps 1–2.

## Dependency graph

```
T-01 ─┬─ T-02 ── T-05 ──┐
      ├─ T-03 ──────────┤
      ├─ T-04 ──────────┼── T-06 ── T-08 ── T-09 ──┐
      └─ T-07 ──────────────────────┘              │
                                                   ├── T-12 ── T-13
T-10 ── T-11 ──────────────────────────────────────┘
```

T-01 and T-10 are the two zero-dependency roots and may run in parallel. T-02/T-03/T-04/T-07 are all parallel-safe given T-01. T-12 is the join point that brings the backend and the built frontend together; T-13 is the final E2E gate.
