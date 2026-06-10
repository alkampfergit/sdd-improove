# Claude Code vs OpenAI Codex: Skills and Subagents

Claude Code and OpenAI Codex support similar high-level ideas: reusable instructions, specialized workers, and repository-level guidance. However, their file formats, invocation behavior, and configuration models are different. A Claude Code skill or subagent definition should not be copied directly into Codex.

---

## Short summary

Claude Code uses:

```text
.claude/skills/<skill>/SKILL.md
.claude/agents/<agent>.md
```

Codex uses:

```text
AGENTS.md
.agents/skills/<skill>/SKILL.md
.codex/agents/<agent>.toml
```

The concepts are similar. The mechanics are not.

---

## Main conceptual overlap

Both systems encourage:

* reusable project guidance
* narrow specialist agents
* clear descriptions
* bounded tool or permission scope
* explicit output formats
* separating orchestration from investigation
* using subagents for verbose or isolated work
* keeping shared conventions in one place

So the design philosophy of a Claude subagent often translates well to Codex.

---

## Main differences

| Topic                 | Claude Code                                                                       | OpenAI Codex                                                               |
| --------------------- | --------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| Repository guidance   | Often handled through Claude-specific project instructions and skills             | `AGENTS.md` is the standard repository guidance file                       |
| Skills                | `.claude/skills/<name>/SKILL.md`                                                  | `.agents/skills/<name>/SKILL.md` and other Codex skill locations           |
| Subagents             | `.claude/agents/<name>.md`                                                        | `.codex/agents/<name>.toml` or `~/.codex/agents/<name>.toml`               |
| Subagent format       | Markdown with YAML frontmatter                                                    | TOML configuration                                                         |
| Required agent fields | Claude-specific frontmatter                                                       | `name`, `description`, `developer_instructions`                            |
| Tool syntax           | Claude tool names such as `Read`, `Grep`, `Glob`, `Bash(...)`                     | Codex configuration and sandbox settings                                   |
| Model names           | Claude model tiers such as Haiku, Sonnet, Opus                                    | OpenAI model names and Codex model settings                                |
| Invocation            | Claude may use descriptions to invoke agents or skills according to its own rules | Codex subagents are normally requested explicitly by the user/main session |
| Nested agents         | Claude document treats subagents as leaf workers                                  | Codex supports configurable agent depth, but one level is usually safer    |
| Forking               | Claude has `context: fork` and `/fork` concepts                                   | Codex uses its own agent/session model, not Claude fork syntax             |

---

## What transfers from Claude to Codex

These Claude design rules are still useful for Codex:

* Use subagents for isolated investigation.
* Use subagents for parallel research.
* Keep each subagent narrow.
* Give each subagent a strict output format.
* Avoid giving broad write permissions to read-only reviewers.
* Put reusable methodology into skills.
* Put stable project conventions in a central place.
* Keep the main session responsible for orchestration and user-facing decisions.

---

## What does not transfer

Do not copy these Claude-specific fields into Codex:

```yaml
permissionMode: plan
tools: Read, Grep, Glob
allowed-tools: Bash(...)
disallowed-tools: Write, Edit
context: fork
agent: Explore
skills: [...]
model: haiku
model: sonnet
model: opus
```

Do not use this Claude path for Codex agents:

```text
.claude/agents/<name>.md
```

Use this instead:

```text
.codex/agents/<name>.toml
```

---

## Example translation

Claude-style subagent:

```markdown
---
name: dependency-scanner
description: Scans dependencies for vulnerabilities.
tools: Read, Grep, Glob, Bash(npm audit *)
model: haiku
permissionMode: plan
---
Return a severity table. Do not modify files.
```

Codex-style equivalent:

```toml
name = "dependency_scanner"
description = "Read-only dependency scanner for known vulnerabilities and outdated packages before releases or security reviews."
model = "gpt-5.4-mini"
model_reasoning_effort = "medium"
sandbox_mode = "read-only"

developer_instructions = """
Inventory dependency manifests and lockfiles.

Return a markdown table:

Severity | Package | Installed | Fixed-in | Evidence | Recommended action

Do not modify files.
"""
```

---

## Practical migration rule

When converting a Claude Code artifact to Codex:

1. Preserve the intent.
2. Discard the Claude syntax.
3. Decide whether it belongs in:

   * `AGENTS.md`
   * `.agents/skills/<skill>/SKILL.md`
   * `.codex/agents/<agent>.toml`
   * or just the current prompt.
4. Rewrite permissions as Codex sandbox/config choices.
5. Rewrite the model field using OpenAI/Codex model settings.
6. Keep the output contract explicit.
7. Keep orchestration in the main Codex session.

---

## Recommended rule of thumb

Use `AGENTS.md` for rules Codex should always know.

Use a skill for a reusable process.

Use a subagent for a bounded specialist worker.

Use an inline prompt for one-time work.
