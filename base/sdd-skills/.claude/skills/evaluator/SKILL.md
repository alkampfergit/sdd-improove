---
name: evaluator
description: >
  Evaluates one complete Spec Driven Design round by inspecting the current
  specs scaffold, then reviewing spec.md, plan.md, and tasks.md together.
  Use when the user asks to "evaluate an SDD round", "score the spec",
  "review spec plan tasks", "assess SDD quality", or wants 1-10 scores and
  concrete improvements for the specification, technical plan, and task list.
---

# Evaluator skill

Evaluate a completed Spec Driven Design round as a verifier. A round is complete when a numbered spec folder under `specs/` contains:

- `spec.md` — what must be true
- `plan.md` — how the feature will be built
- `tasks.md` — the implementation sequence

This skill is read-only by default. Do not rewrite the artifacts unless the user explicitly asks for edits after seeing the evaluation.

## Process

### Step 1 — Locate the round

Use the `specs-scaffold` skill's `list` behavior to understand the current `specs/` directory:

1. Read `specs/specs.md` if present.
2. List direct subfolders matching `specs/NNN_*`.
3. For each folder, check whether `spec.md`, `plan.md`, and `tasks.md` exist.

If the user named a feature or folder, evaluate that folder. If they did not name one:

- If exactly one folder has all three files, evaluate it.
- If multiple folders are complete, evaluate the highest-numbered complete folder and state that choice.
- If no folder is complete, stop and report what is missing per folder. Do not infer missing artifacts from memory.

### Step 2 — Read the project rules and inputs

Read these files before scoring:

1. `.specify/memory/constitution.md` — project principles, stack, forbidden patterns, testing standards
2. Accepted ADRs in `.specify/memory/adrs/`, if the plan references any
3. `specs/<folder>/spec.md`
4. `specs/<folder>/plan.md`
5. `specs/<folder>/tasks.md`
6. `specs/<folder>/interview.md`, if present, to distinguish intentional decisions from gaps

If the constitution is missing, continue the evaluation but cap the plan and tasks scores at 7 because constitutional compliance cannot be verified.

### Step 3 — Score the specification

Score `spec.md` from 1 to 10 using this rubric:

- **9-10**: Clear user value, prioritized INVEST stories, testable Given/When/Then scenarios, EARS-style FRs, measurable technology-agnostic SCs, edge cases, no unresolved critical ambiguity.
- **7-8**: Mostly usable contract with minor ambiguity, weak success criteria, or a few acceptance scenarios that need sharper observable outcomes.
- **5-6**: Understandable intent but stories are coupled, requirements mix implementation with behavior, or several criteria cannot drive tests.
- **3-4**: Major gaps in scope, actors, outcomes, edge cases, or measurable success; downstream planning would require guessing.
- **1-2**: Not a reviewable spec; mostly notes, placeholders, or contradictions.

Check specifically:

- Story priorities make sense, and P1 is independently useful.
- Every acceptance scenario has concrete Given, When, and Then clauses.
- FR IDs are sequential, observable, and technology-agnostic.
- SC IDs are measurable and not disguised implementation details.
- Edge cases cover invalid input, boundaries, absence of data, and failure modes relevant to the feature.
- Open `[NEEDS CLARIFICATION]` markers are acceptable only if they do not block P1 implementation.

### Step 4 — Score the technical plan

Score `plan.md` from 1 to 10 using this rubric:

- **9-10**: Implements the whole spec, complies with constitution and accepted ADRs, makes architectural changes explicit, cites FR/SC IDs beside design decisions, gives concrete module/data/API/deployment details, and names real risks with mitigations.
- **7-8**: Solid implementation guide with small traceability gaps, underdeveloped risks, or minor missing details that an implementer can resolve safely.
- **5-6**: Plausible design but important sections are vague, criterion citations are sparse, ADR/constitution handling is weak, or API/data details are incomplete.
- **3-4**: Requires substantial invention during implementation, contradicts parts of the spec or constitution, or hides load-bearing architecture choices.
- **1-2**: Not an actionable plan; mostly placeholders, generic advice, or incompatible with the spec.

Check specifically:

- Every P1/P2 behavior in `spec.md` is accounted for by a design element.
- The `Constitution check` is real, not boilerplate.
- Referenced ADR links exist and are accepted when needed.
- Project-wide architectural changes are captured as ADRs; local changes are in `Architectural changes`.
- Module layout names concrete files/symbols and aligns with the repository shape.
- Data model, API contract, algorithms, dependencies, deployment plan, and risks are specific enough for implementation.
- The plan avoids speculative work not required by `spec.md`.

### Step 5 — Score the task list

Score `tasks.md` from 1 to 10 using this rubric:

- **9-10**: Tasks are atomic 30-60 minute units, topologically ordered, independently verifiable, each with scoped files, a named acceptance test, explicit dependencies, and Done-when lines tied to FR/SC IDs.
- **7-8**: Implementable sequence with minor size, dependency, or traceability issues.
- **5-6**: Useful breakdown but tasks are uneven, acceptance tests are vague, dependencies are incomplete, or spec coverage is hard to prove.
- **3-4**: Too coarse or too fragmented; implementer would need to re-plan before coding.
- **1-2**: Not an actionable implementation sequence.

Check specifically:

- T-01 has no dependencies and establishes only what later tasks need.
- No task depends on a later task; no cycles are implied.
- Tasks that touch the same files are ordered unless explicitly parallel-safe.
- Each acceptance test maps to the behavior it proves.
- Every required spec behavior is covered by at least one task or intentionally deferred as out of scope.
- No task introduces architecture, dependencies, or features absent from the plan/spec.

### Step 6 — Evaluate round coherence

After scoring each artifact, evaluate the round as a chain:

- **Spec to plan traceability**: every meaningful FR/SC has a corresponding design decision.
- **Plan to tasks traceability**: every material design decision has implementation coverage.
- **Tasks to tests traceability**: every behavior-changing task has a named proving test.
- **Decision hygiene**: constitution and ADR rules are respected without burying deviations in feature docs.
- **Scope control**: later artifacts do not expand beyond the approved spec.

Do not average the three scores mechanically. If one artifact is strong but the chain breaks, call that out.

### Step 7 — Write the evaluation

Return the evaluation in this exact structure:

```markdown
# SDD Round Evaluation: <NNN_slug>

## Scores

| Artifact | Score | Verdict |
|---|---:|---|
| Spec | <1-10>/10 | <one sentence> |
| Plan | <1-10>/10 | <one sentence> |
| Tasks | <1-10>/10 | <one sentence> |

## Highest-Leverage Improvements

1. <Most important improvement across the whole round.>
2. <Second improvement.>
3. <Third improvement.>

## Spec Findings

- **Strength:** <specific evidence from spec.md>
- **Gap:** <specific issue, with section or ID>
- **Improve by:** <concrete rewrite or addition>

## Plan Findings

- **Strength:** <specific evidence from plan.md>
- **Gap:** <specific issue, with section or ID>
- **Improve by:** <concrete rewrite or addition>

## Task Findings

- **Strength:** <specific evidence from tasks.md>
- **Gap:** <specific issue, with task ID or section>
- **Improve by:** <concrete rewrite or addition>

## Traceability

| Chain | Status | Notes |
|---|---|---|
| Spec → Plan | Pass/Partial/Fail | <short note> |
| Plan → Tasks | Pass/Partial/Fail | <short note> |
| Tasks → Tests | Pass/Partial/Fail | <short note> |
| Constitution/ADR compliance | Pass/Partial/Fail | <short note> |

## Score Upgrade Path

- **Spec to <next score>:** <one or two concrete actions>
- **Plan to <next score>:** <one or two concrete actions>
- **Tasks to <next score>:** <one or two concrete actions>
```

Keep findings evidence-based. Prefer citing IDs and headings over broad judgment. If an artifact is excellent, still name the next marginal improvement; a 10/10 means no material improvement is worth doing before implementation.

## What this skill is NOT for

- Writing or rewriting `spec.md`, `plan.md`, or `tasks.md` during the first evaluation pass
- Implementing tasks
- Approving constitutional deviations without accepted ADRs
- Replacing the `constitution-check` skill for a single-artifact compliance audit

