---
name: specify
description: Take a feature description and produce spec.md with user stories satisfying INVEST and acceptance criteria in EARS notation. Asks clarifying questions to surface assumptions before writing. Output goes to specs/<feature-name>/spec.md.
---

# Specify skill

Turn a feature brief into a reviewable specification. The output is a markdown document at `specs/<feature-name>/spec.md` that becomes the contract between the implementer and the verifier — every implementation task and every verification check derives from it.

## Process

### Step 1 — Read the constitution first

Before anything else, read `.specify/memory/constitution.md`. Every spec must respect the constitution's principles, stack, and forbidden patterns.

If the constitution doesn't exist, stop and ask the user to run the `constitution` skill first. **Don't try to spec without one** — you'll write requirements that contradict the project's rules.

### Step 2 — Get the brief

If the user gave you a feature brief at invocation time, use it. Otherwise ask: *"What feature do you want to specify? One to three sentences is enough."*

Pick a short, kebab-case feature name (e.g. `url-shortener`, `password-reset`, `bulk-import`) — that's the directory name under `specs/`.

### Step 3 — Ask the questions that surface assumptions

This is the most valuable part of the skill. The brief is always underspecified. Ask focused questions that draw out what isn't said.

Ask **three to six questions per turn**. After answers, ask follow-ups only if needed. Stop when you have enough to write — perfect is the enemy of done.

Cover at minimum:

- **Inputs** — what does the user provide? Format, validation, constraints?
- **Outputs** — what does the system produce? Status codes, response shapes, side effects?
- **Edge cases** — empty input? Malformed input? Concurrent access? Boundary values?
- **Out of scope** — what's NOT part of v1? (auth? rate limiting? observability? UI?)
- **Quality bars** — latency? error rate? data persistence guarantees?

### Step 4 — Apply INVEST to user stories

Each user story must satisfy **INVEST**:

- **Independent** — implementable without other stories, or dependencies are explicit
- **Negotiable** — describes the goal, not the implementation
- **Valuable** — delivers something a user or stakeholder cares about
- **Estimable** — small enough that someone could size it
- **Small** — fits in a single iteration (a few hours to a day for AI-assisted development)
- **Testable** — has clear acceptance criteria (covered by EARS in the next step)

If a story doesn't satisfy all six, split or rewrite it before writing the file. A story that takes "weeks" or "the whole sprint" is too big.

Use the format: **As a `<role>`, I want `<goal>`, so that `<benefit>`.**

### Step 5 — Apply EARS to acceptance criteria

Every functional requirement must follow one of the five EARS patterns. Number them A1, A2, A3, …

| Pattern | Template | When to use |
|---|---|---|
| **Ubiquitous** | The `<system>` SHALL `<response>` | Always-on facts about the system |
| **Event** | WHEN `<trigger>`, the `<system>` SHALL `<response>` | Behavior on a specific event (most common) |
| **State** | WHILE `<state>`, the `<system>` SHALL `<response>` | Behavior during a long-running condition |
| **Optional** | WHERE `<feature>`, the `<system>` SHALL `<response>` | Behavior conditional on a feature flag |
| **Unwanted** | IF `<trigger>`, THEN the `<system>` SHALL `<response>` | Error handling (second most common) |

Self-check before writing: for each criterion, can you picture the test that proves it? If not, rewrite — usually the trigger or the response is too vague.

### Step 6 — Write the file

Output to `specs/<feature-name>/spec.md` with this structure exactly:

```markdown
# Spec: <Feature Name>

## Overview
<two to four sentences — what is this and why does it exist>

## User stories
- **US1.** As a <role>, I want <goal>, so that <benefit>.
- **US2.** ...

## Functional requirements (EARS)

| ID  | Pattern | Criterion |
|-----|---------|-----------|
| A1  | Event   | WHEN ... |
| A2  | Unwanted | IF ... THEN ... |
| ... | ... | ... |

## Non-functional requirements
- **NFR1.** <latency / availability / persistence target>
- **NFR2.** ...

## Edge cases
- <edge case>
- ...

## Non-goals
- <explicitly out of scope>
- ...

## Success metrics
- <measurable outcome that signals success>
- ...
```

### Step 7 — Hand off

Tell the user the spec is written and offer:

1. **Read it end-to-end before approving.** This is the contract for everything that follows. If you wouldn't sign it, don't approve it.
2. Run the `plan` skill to translate this spec into a technical plan.

Do not proceed automatically.

## What this skill is NOT for

- Implementation details — those go in the plan
- Architectural decisions — also in the plan
- Task breakdown — use `tasks` after `plan`
- Anything that's not a requirement (e.g. "we'll probably use Redis later") — that's a design note, not a spec

## Example: a clean EARS criterion vs a vague one

**Vague** (don't write this):

> *The system shall handle invalid URLs gracefully.*

What's "gracefully"? What counts as "invalid"? You can't write a test.

**EARS** (write this):

> A2 — IF the POSTed body is missing `long_url`, or the value is not a string, or the value does not start with `http://` or `https://`, THEN the service SHALL return HTTP 400 with a JSON body `{"error": "<reason>"}`.

Specific trigger, named subject, observable response, testable.
