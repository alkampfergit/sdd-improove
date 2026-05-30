# ADR-0001: Embedded SQLite calls exempt from the async-everywhere principle

- **Status:** Accepted
- **Date:** 2026-05-22
- **Triggered by:** [plan skill Step 1b for specs/001_url_shortener](../../../specs/001_url_shortener/plan.md) — internal contradiction between Stack recommendation and Principles surfaced while drafting the plan.

## Context

The project constitution names a recommended SQLite driver and, separately, requires that all I/O in the request path be asynchronous:

> "Persistence: SQLite (better-sqlite3 recommended; revisit for SaaS scale via ADR)"

> "Async everywhere. No synchronous I/O in any request path — use async fs, async DB drivers, and async middleware throughout."

`better-sqlite3` is a synchronous driver by design — it exposes only blocking calls and is explicitly marketed as such. Adopting the recommended driver therefore violates the "async DB drivers" half of the async-everywhere principle. Both clauses cannot hold simultaneously, so this is an internal contradiction in the constitution rather than a feature-specific deviation.

The async-everywhere principle was written to protect the event loop from being blocked on slow I/O — network calls, remote disk, large file reads. Embedded SQLite running against a local file is structurally different: a typical read or write completes in well under a millisecond, comparable to a few CPU-bound operations, and the cost of marshaling every call through a worker thread (the only way to make `better-sqlite3` "async") is greater than the cost of just running it inline. The async principle, applied literally to embedded SQLite, harms the very latency it was written to protect.

## Decision

We use `better-sqlite3` synchronously in the request path, and the async-everywhere principle is amended to exempt embedded SQLite calls.

In practice this means: repository functions that wrap `better-sqlite3` operations may be synchronous; services and routes may call them without `await`; the rest of the async-everywhere principle (async `fs`, async middleware, async outbound HTTP) remains in force. Any future move to a network-attached database — Postgres, hosted SQLite, anything remote — falls back under the original async rule and would require a fresh ADR to revisit.

## Consequences

**Positive:**
- Aligns with the constitution's named driver recommendation, removing a contradiction future readers would otherwise have to rediscover.
- Simpler repository code (no promise wrapping, no worker-thread marshaling).
- Faster per-call latency for the embedded SQLite workload this project is sized for (< 100k links, < 100 rps per [spec.md SC-001..SC-003](../../../specs/001_url_shortener/spec.md)).

**Negative / accepted costs:**
- Synchronous DB calls block the event loop for their duration. Acceptable because embedded SQLite calls are sub-ms; revisit if any single query grows beyond a few milliseconds.
- A future migration to a network DB invalidates this exemption and requires a new ADR — but that migration would already require revisiting the Stack section per the constitution's existing "revisit for SaaS scale via ADR" clause, so the trigger is already wired in.
- Engineers reading service/route code will see sync DB calls and must understand the exemption is scoped to embedded SQLite, not a general loosening of the principle.

**Constitution impact:**
- This ADR amends the Principles clause: "Async everywhere. No synchronous I/O in any request path — use async fs, async DB drivers, and async middleware throughout." The amended clause exempts embedded SQLite drivers (currently `better-sqlite3`) from the "async DB drivers" requirement. All other parts of the principle remain in force.

## Alternatives considered

### Use an async driver (e.g. `sqlite` promise wrapper over `sqlite3`)

This would honor the async-everywhere principle literally, but it overrides the constitution's named Stack recommendation (`better-sqlite3`), adds promise-wrapping boilerplate to every repository call, and offers no real-world latency benefit for embedded SQLite — the worker thread hop costs more than the query itself for typical OLTP-shaped reads. Rejected because the principle's intent (protect the event loop from slow I/O) is not served by applying it to sub-ms local-file calls, and because deviating from the named driver would itself require an ADR — the same paperwork with worse ergonomics.

### Wait for `node:sqlite` (Node 22+) to stabilize and use that

The built-in `node:sqlite` is async-friendly and would resolve the contradiction without an exemption, but the constitution pins Node 20+ and `node:sqlite` is still experimental in Node 22 at the time of this decision. Rejected for now because it would require bumping the runtime minimum (a Stack change requiring its own ADR) on an experimental API. Worth revisiting once `node:sqlite` is stable in an LTS that the project has otherwise moved to.

### Leave the contradiction unresolved and pick per-feature

Each feature would re-litigate the same question. Rejected because the contradiction is in the constitution itself, not in any feature — it should be resolved once, at the constitution level, not embedded as a recurring "Constitution check" paragraph in every plan.md.

## Follow-ups

- On Accept, edit `.specify/memory/constitution.md` to annotate the amended Principles clause with a citation back to this ADR: `(amended by [ADR-0001](./adrs/0001-embedded-sqlite-exempt-from-async-principle.md) on 2026-05-22)`.
- Revisit if: the project migrates off embedded SQLite; or `node:sqlite` becomes stable in the project's runtime baseline; or profiling shows a single query exceeding a few milliseconds (which would invalidate the sub-ms premise this exemption rests on).
