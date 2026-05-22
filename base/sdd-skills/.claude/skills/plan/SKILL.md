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

### Step 1b — Constitution conflict gate (do this before drafting anything)

Before writing a single line of plan content, enumerate every place where the plan you are about to write would **deviate from, weaken, or contradict** the constitution. Be honest: look at the constitution's Principles, Stack, Forbidden patterns, and Testing standards sections one by one, and ask "does the design I have in mind comply, line by line?"

Two failure modes to watch for:

1. **External conflicts** — your plan needs to use a different library, runtime version, framework, or pattern than the constitution names. (Example: constitution says SQLite; your plan needs Postgres.)
2. **Internal contradictions** — the constitution itself contains two rules that cannot both be satisfied by any plan. (Example: constitution recommends a synchronous driver *and* forbids sync I/O in the request path. Both can't hold.)

For each conflict you find, choose exactly one of:

- **(a) Revise the plan to comply.** Pick the design that satisfies the constitution. This is the default — try it first.
- **(b) Formalize the deviation via an ADR.** Invoke the `adr` skill to draft the decision *before* writing `plan.md`. The `adr` skill writes the file as `Status: Proposed` and asks the user to Accept or Reject. **Wait for that decision** — do not write `plan.md` until the ADR resolves. Then:
  - If the user **Accepts**, the ADR flips to `Accepted` and `plan.md` cites it by ID, with no inline justification of the deviation. The ADR — not the plan — is where the deviation is argued and consequences are owned.
  - If the user **Rejects**, the ADR file stays on disk as `Status: Rejected` (audit trail), and you must fall back to option (a): revise the design to comply with the constitution as written. Do not write the deviating plan anyway.
- **(c) Stop and ask the user** which of (a) or (b) they want. If the conflict is small and the answer is obvious, just do (a). If the conflict touches stack or principles, default to surfacing it.

**Do not "flag and continue."** A `plan.md` paragraph that says "the constitution says X but we'll do Y because Z" is not a valid resolution — it bakes a deviation into a per-feature document that future readers will not know to consult. Deviations live in ADRs; plans cite them.

If you find no conflicts, say so explicitly in the plan's `## Constitution check` section (added in Step 5). "No conflicts" is a useful finding for the verifier.

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

### Step 3b — Log every technical question and answer to `interview.md`

After each round of questions, append the exchange to `specs/<feature-name>/interview.md` under a top-level section titled `## Technical questions` (created on first append, reused on subsequent rounds). The `specify` skill owns the rest of that file; this skill only appends — it never edits or removes earlier content.

Format for each entry, mirroring the existing `Q<n>` style in `interview.md`:

```markdown
## Technical questions

### TQ1 — <short label, e.g. "SQLite driver">
**Reasoning shown:** <one line summarizing the options/trade-offs you presented>
**Question:** <the exact question asked>
**Answer:** <the user's selected option, verbatim — include any "Other" free-text>
```

Number entries `TQ1`, `TQ2`, … across the whole feature (continue numbering on later plan runs; do not restart). If the user overrides an earlier technical decision in a later turn, add a new `TQ<n>` entry that supersedes it rather than editing the earlier one — the history matters.

Write the append immediately after the user answers, before drafting the plan, so the record exists even if plan generation is interrupted. If `interview.md` does not exist (rare — `specify` should have created it), create it with just the `## Technical questions` section and proceed.

### Step 4 — Cite spec criterion IDs

Every design decision should reference the spec criterion it satisfies. For each major design choice in the plan, write `(satisfies A3)` or `(supports NFR2)`. This makes the plan auditable end-to-end and gives the verifier explicit handles.

### Step 5 — Write the file

Output to `specs/<feature-name>/plan.md` with this structure:

```markdown
# Plan: <Feature Name>

> Technical design satisfying spec.md. Cite spec criterion IDs (A1, A2, NFR1, ...) next to design decisions.

## Architecture overview

<two to four paragraphs — the shape of the solution. A small ASCII diagram is welcome if it helps.>

## Constitution check

<Per Step 1b. One of:
- "No conflicts — design complies with the constitution as written."
- "Deviations resolved via ADR(s): ADR-NNNN — <title>, ADR-MMMM — <title>." (List each ADR; do not restate its content here.)
If unresolved deviations remain, you should not have reached this step. Go back.>

## ADRs referenced

<List the ADR IDs this plan depends on. Format: `- [ADR-NNNN](../../.specify/memory/adrs/NNNN-slug.md) — one-line summary.` Empty list is valid.>

## Architectural changes

<List any changes to the existing architecture this feature requires. "None — fits existing patterns" is a valid and useful answer. If there are changes, name each one and explain why. Note: an architectural change is distinct from a constitution conflict — the former adds new shape inside the constitution's rules; the latter changes the rules themselves and requires an ADR.>

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

### Step 5b — Post-draft ADR sweep (do this before handoff)

Step 1b ran *before* drafting and caught conflicts you saw coming. But design decisions crystallize *during* drafting — the user's TQ answers pick specific libraries, you pin specific versions, you choose specific deployment shapes. Each of those is a fresh chance to deviate from the constitution that the pre-draft gate had no way to see. So before handing off, re-walk the **drafted** plan against the constitution one more time.

Open `plan.md` and the constitution side-by-side. For every section that names a concrete choice — `Module layout`, `Data model`, `API contract`, `Algorithms / key decisions`, `Dependencies`, `Deployment plan` — ask:

- Does this choice match what the constitution's Stack section names? If the constitution says "Persistence: X (Y recommended)" and you picked Z, that is a deviation **even if Z is technically allowed** by another clause. "Recommended" choices in the Stack section are the project's defaults; overriding them is the kind of decision the constitution wants formalized.
- Does this choice violate any Principle, Forbidden pattern, or Testing standard?
- Does the user's answer to a TQ question pick something other than the constitution's named default? (TQ answers are the most common source of post-draft deviations — the user picked an option that satisfied one rule but quietly overrode another.)

For each deviation found, the resolution rules are the same as Step 1b: revise the plan to comply, write an ADR via the `adr` skill, or stop and ask. **Do not paper over a deviation by writing a "Constitution check" paragraph that argues why the deviation is fine** — that's the same anti-pattern Step 1b warned against, just discovered later.

Common shapes this step catches that Step 1b cannot:

- The user picked a library option in TQ that is not the one the Stack section recommends.
- A version pin in `Dependencies` requires a runtime newer than the constitution's stated minimum.
- A choice in `Algorithms` introduces a pattern the Forbidden section names (e.g. a sync DB call inside what looked async at the gate).
- The chosen deployment shape requires a process/container/job the constitution's deployment assumptions don't include.

If the sweep finds nothing, say so explicitly in your handoff message ("post-draft ADR sweep: no new deviations"). That's a useful signal for the verifier.

If the sweep finds one or more deviations and an ADR is the right resolution, hand off to the `adr` skill **now**, before Step 6. The plan's `Constitution check` and `ADRs referenced` sections must be updated to cite the resulting ADR(s) once Accepted — same flow as Step 1b.

### Step 6 — Hand off

Tell the user the plan is written and offer:

1. **Read the `Architectural changes` and `Risks` sections carefully.** Those are where decisions get baked in. Push back on anything you disagree with before approving.
2. Recommended: run the `constitution-check` skill on `plan.md` before decomposing — it audits the plan against the constitution + accepted ADRs and catches speculative design that the plan-writing model rationalized. The Step 5b sweep is your own pass; `constitution-check` is the independent fresh-context audit. Both are worth running because the author of an artifact is structurally the worst auditor of it.
3. Run the `tasks` skill to decompose this plan into atomic implementation tasks.

## What this skill is NOT for

- Acceptance criteria — those live in spec.md
- Task-level breakdown — use `tasks` next
- Code generation — the implementer will do that, task by task, using a different loop
- Speculative future features — out of scope is out of scope

## Example: when to flag an architectural change

Spec says: *"WHEN a user requests a password reset, the system SHALL email a reset link valid for 1 hour."*

If the project already sends emails: **no architectural change.** Just a new email template and a new endpoint.

If the project has never sent an email before: **architectural change.** You're adding a new dependency on an outbound mail provider, a new failure mode (mail delivery), new credentials to manage, new monitoring. Flag it explicitly. The implementer needs to know they're not just writing a route handler.
