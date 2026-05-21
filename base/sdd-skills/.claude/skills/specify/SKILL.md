---
name: specify
description: >
  Produce a feature specification at specs/<folder>/spec.md by filling in the
  project template at .specify/templates/spec.template.md. Prioritizes user
  stories (P1 highest, independently testable), writes acceptance scenarios in
  Given/When/Then form, captures functional requirements as FR-xxx MUST
  statements, and defines measurable SC-xxx success criteria. Asks clarifying
  questions to surface assumptions before writing. Use when the user wants to
  "write a spec", "specify a feature", "draft a spec", or after running the
  specs-scaffold skill to create the folder.
---

# Specify skill

Turn a feature brief into a reviewable specification. The output is a populated copy of [`.specify/templates/spec.template.md`](../../../.specify/templates/spec.template.md), written as `specs/<folder>/spec.md`. That file becomes the contract between the implementer and the verifier — every implementation task and every verification check derives from it.

The **template is the structural source of truth**. This skill provides the workflow for filling it in; it does not invent its own section layout.

## Process

### Step 1 — Read the constitution

Read `.specify/memory/constitution.md`. Every spec must respect the constitution's principles, stack, and forbidden patterns.

If the constitution doesn't exist, stop and ask the user to run the `constitution` skill first. **Don't try to spec without one** — you'll write requirements that contradict the project's rules.

### Step 2 — Read the template

Read `.specify/templates/spec.template.md`. That file defines the section structure, headings, and placeholder syntax. Your job is to populate it — never invent new sections, rename headings, or change the requirement/success-criterion numbering scheme.

If the template is missing, stop and ask the user to restore it before proceeding.

### Step 3 — Locate the spec folder

Numbered spec folders are owned by the `specs-scaffold` skill (e.g. `specs/001_url_shortener/`). If the target folder doesn't exist, stop and tell the user to run `specs-scaffold` first — that skill picks the next number, creates the directory, drops a stub `spec.md` with a back-link to the root index, and updates `specs/specs.md`.

This skill **overwrites** that stub `spec.md` with the populated template. The template carries the back-link forward, so the final file still links back to the root index.

### Step 4 — Get the brief

If the user gave you a feature brief at invocation, use it. Otherwise ask: *"What feature do you want to specify? One to three sentences is enough."*

### Step 5 — Ask the questions that surface assumptions

The brief is always underspecified. Ask focused questions that draw out what isn't said. **Three to six questions per turn**; follow-ups only if needed; stop when you have enough to write.

Cover at minimum:

- **Inputs** — what does the user provide? Format, validation, constraints?
- **Outputs** — what does the system produce? Status codes, response shapes, side effects?
- **User journeys** — which flows matter, and which one is the smallest viable slice (P1)?
- **Edge cases** — empty input? Malformed input? Concurrent access? Boundary values?
- **Out of scope** — what's NOT part of v1? (auth? rate limiting? observability? UI?)
- **Success criteria** — what measurable outcome proves this works? (latency, completion rate, error budget)

If something stays unclear after asking, leave it in the spec as `[NEEDS CLARIFICATION: <what is unclear>]` rather than guessing.

### Step 6 — Prioritize user stories

Each story is **P1, P2, P3, ...** where P1 is most critical. The key constraint: every story must be **independently testable** — if you implemented only that story, it would still deliver a usable slice. P1 alone should be a viable MVP.

For each story, you'll need:

- **Brief title** + priority tag
- **Journey** in plain language
- **Why this priority** — the value and why it ranks here
- **Independent Test** — how to verify the story on its own
- **Acceptance Scenarios** as Given/When/Then (one or more)

If a story can only be tested in combination with another, it's not really independent — split it or merge it.

### Step 7 — Write acceptance scenarios in Given/When/Then

Every acceptance scenario follows this shape:

> **Given** `<initial state>`, **When** `<action>`, **Then** `<expected outcome>`.

Self-check before writing each scenario: can you picture the test that proves it? If not, rewrite — usually the *Given* or the *Then* is too vague. "Then the system handles it gracefully" is not a test.

### Step 8 — Write functional requirements

Number them **FR-001, FR-002, ...** sequentially. Each is a MUST statement, technology-agnostic.

Patterns to use:

- **System MUST** `<capability>` — e.g. "System MUST persist redirect counts across restarts."
- **Users MUST be able to** `<interaction>` — e.g. "Users MUST be able to retrieve click stats by short code."
- **System MUST NOT** `<forbidden behavior>` — when prohibition is the requirement.

Mark unknowns inline: `[NEEDS CLARIFICATION: <what is unclear>]`. Don't fabricate values that weren't discussed.

### Step 9 — Define success criteria

Number them **SC-001, SC-002, ...**. Each must be **measurable** and **technology-agnostic** — no library names, no framework specifics, no implementation hints.

Good: *"Users complete signup in under 2 minutes on first attempt."*
Bad: *"The React form is fast."*

Aim for 3–5 criteria covering the dimensions that actually matter (user outcome, throughput, error rate, satisfaction, business metric).

### Step 10 — Write the file

Copy the structure of `.specify/templates/spec.template.md` to `specs/<folder>/spec.md`, replacing every `[bracket placeholder]` with concrete content. This overwrites the stub left by `specs-scaffold`. Keep the back-link line (`[↑ Back to specs index](../specs.md)`) exactly as it appears in the template. Substitutions:

| Placeholder | Fill with |
|---|---|
| `[FEATURE NAME]` | Title-cased feature name derived from the folder slug |
| `[###-feature-name]` | Folder number + kebab-cased slug (e.g. `001-url-shortener`) |
| `[DATE]` | Today's date in `YYYY-MM-DD` |
| `"$ARGUMENTS"` | The user's original brief, verbatim |
| Story/edge/FR/SC placeholders | Concrete content from Steps 5–9 |

Preserve the template's headings, section order, and numbering format exactly. Remove unused optional sections (e.g. *Key Entities* if the feature has no data model). Remove the template's `<!-- comments -->` from the final spec.

### Step 11 — Hand off

Tell the user the spec is written and offer:

1. **Read it end-to-end before approving.** This is the contract for everything that follows. If you wouldn't sign it, don't approve it.
2. Run the `plan` skill to translate this spec into a technical plan.

Do not proceed automatically.

## What this skill is NOT for

- Implementation details — those go in the plan
- Architectural decisions — also in the plan
- Task breakdown — use `tasks` after `plan`
- Folder creation or numbering — use `specs-scaffold`
- Anything that's not a requirement (e.g. "we'll probably use Redis later") — that's a design note, not a spec

## Example: a clean Given/When/Then vs a vague one

**Vague** (don't write this):

> *Given a bad URL, When the user submits it, Then the system handles it gracefully.*

What's "bad"? What's "gracefully"? You can't write a test.

**Concrete** (write this):

> *Given the request body is missing `long_url`, or the value is not a string, or the value does not start with `http://` or `https://`, When the client POSTs to `/api/v1/links`, Then the service returns HTTP 400 with a JSON body `{"error": "<reason>"}`.*

Specific trigger, named subject, observable response, testable.
