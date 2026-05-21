---
name: plan
description: Take a spec.md and produce plan.md with architecture, data model, API contracts, dependencies, deployment plan, and risks. Identifies architectural changes the new feature requires. Asks about non-obvious decisions only. Output goes to specs/<feature-name>/plan.md.
---

# Plan skill

Translate a spec into a technical plan. The plan answers HOW; the spec answered WHAT. Output is a markdown document at `specs/<feature-name>/plan.md` that the implementer follows and the verifier references.

## Process

### Step 1 — Read the inputs in this order

1. `.specify/memory/constitution.md` — the rules you can't break
2. `specs/<feature-name>/spec.md` — the contract you must satisfy
3. `AGENTS.md` — operational conventions (file layout, build commands)
4. The current codebase structure if this is brownfield work

If `spec.md` doesn't exist, stop and tell the user to run `specify` first. Don't try to plan against a feature that hasn't been specified.

### Step 2 — Detect architectural changes (do this before writing)

Before anything else, decide whether this feature requires changes to the **existing architecture**, or whether it fits within it. Look for these signals:

- **New persistence concerns** — new tables, new external services, new caches, new file stores
- **New traffic shapes** — long-running jobs, websockets, batch processing, scheduled work
- **New trust boundaries** — auth, multi-tenant data isolation, untrusted input
- **New deployment requirements** — additional processes, additional containers, scheduled jobs
- **New cross-cutting concerns** — observability, rate limiting, audit logging, internationalization

If the feature triggers any of these, **flag it explicitly** in the plan's `## Architectural changes` section. The implementer needs to know whether they're adding a feature inside the existing architecture or changing the architecture itself.

If the answer is "none — this fits existing patterns", say so. That's a valid and useful finding.

### Step 3 — Ask only the non-obvious questions

Most of the plan can be derived from spec + constitution. Ask the user only when:

- A design decision has genuine trade-offs the spec is silent on (e.g. "store the full URL or hash it?")
- A library choice isn't obvious from the constitution (e.g. "Express or Fastify?" — only ask if neither is mentioned)
- The deployment target needs special handling (e.g. "this needs a writable path on Azure App Service — where?")
- The migration path matters (e.g. "how do existing users see this new column?")

Ask **two to four questions per turn**. Never ask things you can infer from the spec or the constitution.

### Step 4 — Cite spec criterion IDs

Every design decision should reference the spec criterion it satisfies. For each major design choice in the plan, write `(satisfies A3)` or `(supports NFR2)`. This makes the plan auditable end-to-end and gives the verifier explicit handles.

### Step 5 — Write the file

Output to `specs/<feature-name>/plan.md` with this structure:

```markdown
# Plan: <Feature Name>

> Technical design satisfying spec.md. Cite spec criterion IDs (A1, A2, NFR1, ...) next to design decisions.

## Architecture overview

<two to four paragraphs — the shape of the solution. A small ASCII diagram is welcome if it helps.>

## Architectural changes

<List any changes to the existing architecture this feature requires. "None — fits existing patterns" is a valid and useful answer. If there are changes, name each one and explain why.>

## Module layout

| File | Exports | Depends on |
|------|---------|------------|
| `<path>` | `<symbols>` | `<external/internal>` |
| ... | ... | ... |

## Data model

<DDL for new tables, schema for new messages, or "no data model changes — uses existing X". Include CREATE TABLE statements, JSON schemas, or message shapes as appropriate.>

## API contract

<One block per endpoint or interface. For each: method + path, request shape, response shape, status codes. Reference A-IDs from the spec.>

## Algorithms / key decisions

<Short notes on non-obvious choices — collision retry strategy, sort order, caching policy, etc. One paragraph each, with the reasoning.>

## Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| <name> | <semver> | <one line> |

## Deployment plan

<What changes about how this feature is deployed. Specific commands if relevant. Configuration values that need to be set. "No deployment changes" is valid if true.>

## Risks and mitigations

| ID | Risk | Mitigation |
|----|------|------------|
| R1 | <risk> | <how it's handled, or "accepted — see X"> |
| ... | ... | ... |

## Out of scope

<What this plan does NOT cover, even if related. Helps the implementer not over-build.>
```

### Step 6 — Hand off

Tell the user the plan is written and offer:

1. **Read the `Architectural changes` and `Risks` sections carefully.** Those are where decisions get baked in. Push back on anything you disagree with before approving.
2. Run the `tasks` skill to decompose this plan into atomic implementation tasks.

## What this skill is NOT for

- Acceptance criteria — those live in spec.md
- Task-level breakdown — use `tasks` next
- Code generation — the implementer will do that, task by task, using a different loop
- Speculative future features — out of scope is out of scope

## Example: when to flag an architectural change

Spec says: *"WHEN a user requests a password reset, the system SHALL email a reset link valid for 1 hour."*

If the project already sends emails: **no architectural change.** Just a new email template and a new endpoint.

If the project has never sent an email before: **architectural change.** You're adding a new dependency on an outbound mail provider, a new failure mode (mail delivery), new credentials to manage, new monitoring. Flag it explicitly. The implementer needs to know they're not just writing a route handler.
