# SDD Skills for Claude Code and Codex CLI

Four reduced-form skills that drive a Spec-Driven Development session end-to-end:

| Skill | Phase | Output |
|---|---|---|
| `constitution` | Project setup | `.specify/memory/constitution.md` |
| `specify` | Feature scoping | `specs/<feature>/spec.md` |
| `plan` | Technical design | `specs/<feature>/plan.md` |
| `tasks` | Decomposition | `specs/<feature>/tasks.md` |

These are course-friendly versions inspired by GitHub Spec Kit's `/constitution`, `/specify`, `/plan`, `/tasks`. Substantial enough to produce real, reviewable artifacts; light enough to teach in a workshop.

Every skill **asks questions when something isn't clear** rather than inventing answers. That's the point — the questions surface unstated assumptions before they become bugs.

## What's in this package

```
sdd-skills/
├── README.md
└── .claude/
    └── skills/
        ├── constitution/SKILL.md
        ├── specify/SKILL.md
        ├── plan/SKILL.md
        └── tasks/SKILL.md
```

## Install

### Claude Code

Drop the `.claude/` folder into the root of any project. Claude Code auto-discovers skills under `.claude/skills/`.

```bash
# from your project root
cp -r path/to/sdd-skills/.claude .
```

The skills are now available in any session running in this directory.

### Codex CLI

Codex looks for skills under `~/.codex/skills/`. Copy the four skill folders there:

```bash
mkdir -p ~/.codex/skills
cp -r path/to/sdd-skills/.claude/skills/* ~/.codex/skills/
```

Skills are now available globally across all your Codex sessions.

### Want slash commands instead?

These are skills, not slash commands. If your team prefers `/specify <brief>` over "use the specify skill," conversion is trivial — copy each `SKILL.md`'s body into `.claude/commands/<name>.md`, prepend a YAML frontmatter with `description` and `argument-hint`, and reference `$ARGUMENTS` where the brief should land.

## Use

### Sequence

The skills are designed to run in order:

1. **`constitution`** — once per project, before any feature work.
2. **`specify`** — once per feature, to translate a brief into a spec with INVEST user stories and EARS acceptance criteria.
3. **`plan`** — once per feature, to design the technical solution from the spec.
4. **`tasks`** — once per feature, to decompose the plan into atomic implementation work.

Then implement task by task using the test-first loop (failing test → make it pass → commit). That part doesn't need a skill.

### Invocation

In Claude Code, either of these works:

```
> use the constitution skill
> /constitution
```

In Codex CLI, the same patterns work. If you want to pass a brief inline:

```
> use the specify skill — feature: a notification service that emails users when their report is ready
```

## Worked example: end-to-end on the URL shortener

This is the canonical example used in the SDD course. The companion **URL Shortener Reference Implementation** zip contains the four artifacts these skills should produce, plus a working Node.js + Express + SQLite implementation.

### Step 1 — `constitution`

Run with a brief like:

> *"Build a URL shortener service in Node.js. Express + better-sqlite3. Deployable to Azure App Service. No auth, no UI, no external databases. Library-first design — business logic separate from HTTP layer."*

Expect 1–2 clarifying questions (typically about testing standards and which forbidden patterns matter most). Output: `.specify/memory/constitution.md` with sections Principles, Stack, Forbidden, Testing standards.

### Step 2 — `specify`

Run with a brief like:

> *"Build a URL shortener: POST a long URL, get a short code. GET the short code redirects to the original. Track click counts."*

Expect 4–8 clarifying questions covering: short-code length and alphabet, validation rules (which URL schemes?), concurrent redirect behavior, expiration policy, persistent storage path on Azure, observability scope.

Output: `specs/url-shortener/spec.md` with INVEST user stories US1–US3 and EARS acceptance criteria A1–A7.

### Step 3 — `plan`

The skill reads the constitution and the spec, then writes the technical plan. For this feature, expect the **Architectural changes** section to flag persistent storage location on Azure App Service (`/home` is durable, `/home/site/wwwroot` is not), and the **Risks** section to call out collision retry, click-counter race conditions, and the better-sqlite3 native build requirement.

Output: `specs/url-shortener/plan.md` with sections Architecture, Architectural changes, Module layout, Data model, API contract, Algorithms, Dependencies, Deployment plan, Risks, Out of scope.

### Step 4 — `tasks`

Output: `specs/url-shortener/tasks.md` with T-01 through T-08:

```
T-01 — Project skeleton + Jest config
T-02 — Library: short-code generator
T-03 — Library: SQLite repository
T-04 — API: POST /api/v1/links
T-05 — API: GET /:code redirect + click counter
T-06 — API: GET /api/v1/links/:code/stats
T-07 — Error handling: 400 invalid URL, 404 unknown code
T-08 — Deploy to Azure App Service
```

Then implement task by task, test-first, until all eight are committed and the live Azure URL responds.

## What these skills are NOT

- **Not a replacement for GitHub Spec Kit.** Spec Kit's full pipeline has more features — constitution amendments, ADRs, branching strategy, multi-feature management. These four skills cover the core loop.
- **Not opinionated about your stack.** Skills work for Node, Python, Go, Rust, anything. The constitution captures the stack; everything downstream respects it.
- **Not magic.** Output quality equals input quality. Garbage in, garbage out. The clarifying questions exist precisely because the brief is always underspecified.

## Customizing

Each `SKILL.md` is plain markdown. To customize:

- **Tighten the question discipline** — adjust the "Ask N questions per turn" guidance.
- **Change the output structure** — edit the markdown template in the "Write the file" step.
- **Add domain-specific rules** — for a regulated domain, the `specify` skill could require a "Compliance considerations" section. For a research codebase, the `plan` skill could require a "Reproducibility plan" section.

If you make changes that work well for your team, version them in your repo's `.claude/skills/` and let them evolve with the project.

## Related materials

These skills pair with the SDD course materials. Specifically:

- **URL Shortener Reference Implementation** (separate zip) — the four artifacts these skills should produce, plus a working implementation.
- **URL Shortener Lab Worksheet v2** (docx) — the participant-facing lab that runs through the full sequence using these skills.
- **SDD with Claude Code 4-hour course deck** (pptx) — the presentation that frames the methodology.

## License

Provided as instructional material for the SDD course. Use freely for teaching, training, and adapting to your team.
