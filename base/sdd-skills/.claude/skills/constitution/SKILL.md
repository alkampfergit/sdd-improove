---
name: constitution
description: Generate a project constitution from a brief description. Captures slow-changing principles, stack rules, forbidden patterns, and testing standards. Use once per project before any spec is written. Re-run only when the project changes direction. Output goes to .specify/memory/constitution.md.
---

# Constitution skill

Create the project's constitution — a markdown document at `.specify/memory/constitution.md` that captures the non-negotiables governing every spec, plan, and task that follows.

The constitution is the **slow-changing layer**. AGENTS.md changes when build commands change. The constitution changes only when the project changes direction. Treat amendments as ADR territory.

## Process

### Step 1 — Read the room first

Before asking the user anything, look around. Most "obvious" answers are already on disk:

- Read `AGENTS.md`, `CLAUDE.md`, or `README.md` at the repo root if they exist
- Skim `package.json`, `pyproject.toml`, `Cargo.toml`, `go.mod` for stack hints
- Glance at the directory layout for architectural cues

Don't ask the user about anything you can already see.

### Step 2 — Ask only what you can't infer

Ask **one to three focused questions per turn**. Never dump a whole questionnaire. Cover only the gaps:

- **Purpose** — one to three sentences. What does this project do? Who is it for?
- **Stack details that aren't obvious** — versions, key library choices, target deployment.
- **Architectural style** — library-first / monolith / serverless / microservices / something else.
- **Forbidden patterns** — what must this project NEVER do? (e.g. "no synchronous I/O in route handlers", "no external databases beyond SQLite", "no client-side rendering")
- **Testing minimum** — unit only? unit + integration? characterization tests for legacy seams?

If the user gave you a brief at invocation time, use it as input. Don't re-ask things they already told you.

### Step 3 — Push back when something contradicts

- "No auth" + multi-tenant data → flag the contradiction.
- "Serverless deployment" + "SQLite for persistence" → flag the contradiction (SQLite needs persistent local disk).
- "Test-first" + "no testing infrastructure" → flag the contradiction.

If the user can't name any forbidden patterns, don't invent. Write `TBD — add via ADR` and move on.

### Step 4 — Write the file

Output to `.specify/memory/constitution.md`. Use this structure exactly:

```markdown
# Project Constitution — <name>

> The non-negotiables of this project. Slow-changing. Modify only via ADR.

## Principles

- **<short name>.** <one or two sentences explaining what and why>
- ... (3 to 6 total — fewer is better than more)

## Stack

- **Runtime:** <language and version>
- **Framework:** <if applicable>
- **Persistence:** <if applicable>
- **Testing:** <runner and key libraries>
- ... (other rows as appropriate)

## Forbidden

- <pattern>
- ... (3 to 8 items)

## Testing standards

- <rule about test types and structure>
- <rule about CI gate — e.g. "npm test must exit 0 before commit">
- <rule about naming or coverage>
```

### Step 5 — Hand off

After writing, tell the user the file is created and offer two next steps:

1. Create or update `AGENTS.md` at the repo root to `@import` the constitution. (`AGENTS.md` holds operational details — build commands, file layout, day-to-day conventions — that change too often to live in the constitution.)
2. Run the `specify` skill to scope the first feature.

Do not run either automatically.

## What this skill is NOT for

- Per-feature requirements — use `specify`
- Implementation details — use `plan`
- Day-to-day operational facts (`npm test`, file layout) — those go in AGENTS.md
- Anything that changes weekly — that's not constitutional

## Example brief that should produce a clean constitution

> *"Build a URL shortener service in Node.js. Express + SQLite + better-sqlite3. Deployable to Azure App Service Linux, Node 20 runtime. No auth, no UI. Library-first design — business logic separate from HTTP layer."*

That brief is enough to write a 30-line constitution with at most one or two clarifying questions (likely about testing and forbidden patterns).
