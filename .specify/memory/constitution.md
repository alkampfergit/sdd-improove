# Project Constitution

> The non-negotiables of this project. Slow-changing. Modify only via ADR.

A customer-facing SaaS product with a Node.js/Express backend and React frontend. SQLite is the chosen persistence layer — appropriate for single-instance deployments; revisit via ADR if write concurrency or horizontal scaling becomes a requirement.

## Principles

- **Layered architecture.** Routes delegate to services; services delegate to repositories. No layer reaches past the one below it.
- **Async everywhere.** No synchronous I/O in any request path — use async fs, async DB drivers, and async middleware throughout. (amended by [ADR-0001](./adrs/0001-embedded-sqlite-exempt-from-async-principle.md) on 2026-05-22 to exempt embedded SQLite drivers — currently `better-sqlite3` — from the async-DB-drivers requirement; all other parts of the principle remain in force.)
- **Simplicity over cleverness.** Write the minimum code that satisfies the requirement. Abstractions must earn their place; single-use generalization is banned.
- **Secrets out of code.** All credentials, API keys, and environment-specific values come from environment variables. Nothing sensitive is committed.
- **Fail loudly.** Prefer explicit errors and early returns over silent fallbacks. Swallowed errors hide bugs.

## Stack

- **Runtime:** Node.js 20+
- **Backend framework:** Express
- **Persistence:** SQLite (better-sqlite3 recommended; revisit for SaaS scale via ADR)
- **Frontend:** React 18+
- **Testing (backend):** Jest + Supertest
- **Testing (E2E):** Playwright
- **Package manager:** npm (or pnpm — TBD)

## Forbidden

- **No DB queries in route handlers.** All database access must go through a repository layer.
- **No secrets in source code.** Credentials and keys live in environment variables only; never committed.
- **No sync I/O in request path.** Use `fs/promises`, async DB calls, and async middleware throughout. (amended by [ADR-0001](./adrs/0001-embedded-sqlite-exempt-from-async-principle.md) on 2026-05-22 to exempt embedded SQLite drivers — currently `better-sqlite3`.)
- **No speculative abstractions.** Don't build "flexibility" or "configurability" that no current feature requires.
- **No silent error swallowing.** Don't catch and ignore errors without logging and either re-throwing or returning a proper error response.

## Testing standards

- Business logic in services must have unit tests; use a real (in-memory or file-based test) SQLite database for repository tests — no mocking the DB.
- API routes must have integration tests via Supertest that exercise the full Express stack.
- E2E tests with Playwright cover critical user flows (auth, core happy paths); cosmetic flows are optional.
- `npm test` must exit 0 before any commit reaches the main branch.
- Test files live adjacent to the code they test (`.test.ts` / `.spec.ts` suffix) or in a sibling `__tests__` folder — no flat top-level `tests/` directory.
