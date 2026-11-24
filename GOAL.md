# Goal

Advance [PLAN.md](PLAN.md) through a green PR and merge it.

## Objective

- Carry forward any in-progress work.
- Address any [Open Issues](https://github.com/wallstop/NovaSharp/issues), paginated, to get a full list. If you only get 50, you have not gotten everything, ordered by most-gameplay impacting first.
- Drive any open dependency to green (incorporate) if relevant.
- Ensure main CI is green; if it is not, RCA the issue and implement a comprehensive fix.
- See if there were any draft/in-progress PRs from previous sessions and drive those to completion.
- Drive `PLAN.md` improvements as far forward as possible towards accomplishing the full work for the next achievable milestone(s) - do not over-scope, keep the target goal within reason. Then, open a PR that achieves a fully green state (all CI checks passing, all reviewer feedback addressed), then merge it. Do not explicitly trigger reviews.
- Aggregate all of one session's work into one PR, do not stack PRs.
- Prefer vscode's github connector extension, then git, then gh for github interactions. Do not change org policies. We have reader and write apps available.
- Open detailed issues for any issues or follow-up items.

## API Evolution Policy

- Treat NovaSharp as pre-adoption: there are no external consumers whose host API
  compatibility must be preserved.
- Freely rename, move, redesign, or remove public APIs when doing so improves Lua
  correctness, performance, memory use, Unity support, or the resulting API.
- Do not add or retain legacy APIs, compatibility shims, forwarding wrappers,
  obsolete aliases, migration adapters, or deprecation windows. Update all
  repository-owned callers, tests, samples, tooling, and documentation atomically.
- This freedom applies to NovaSharp's host-facing APIs. It does not relax exact Lua
  version behavior or supported-platform compatibility requirements.

## Work Style

- **Task-based, red-green, data-backed, exploratory, science-driven.**
- **Session length target: under 2 hours.** Scope each session to one shippable
  PR that completes a falsifiable slice (see [plan-maintenance](.llm/skills/plan-maintenance/SKILL.md)).
  If the remaining work cannot finish in time, stop at a green checkpoint and
  hand off through `PLAN.md`, `progress/`, and issues instead of over-running.
- Commit and push freely. Small, coherent commits.
- Address all non-trivial reviewer feedback and CI failures before moving to the next task.
- When CodeRabbit reports a rate limit, proceed with the merge as soon as CI passes on the final commit and all actionable review feedback is addressed. If an earlier CodeRabbit change request still blocks the merge, verify its fixes, resolve its threads, and dismiss that review with links to the fixes and validation.

## Fix Philosophy

| Failure type                | Fix target           |
| --------------------------- | -------------------- |
| Production bug              | Production code      |
| Test bug                    | Test code            |
| Fragile / unnecessary check | Remove or rewrite it |

- Prefer **data-driven tests** over repeated similar test cases.
- Add **diagnostics** when an issue is hard to understand without them.
- When a bug is found, **sweep the codebase** for the same class of problem and fix all affected paths.
- **Simplify aggressively.** Do not add tests, scripts, or abstractions unless they prevent a real, recurring issue.

## Quality Bar

Every change must be:

- **Robust and reliable** not fragile or coupled to implementation details.
- **Complete** the whole class of problem is addressed, not just the one example.
- **Non-bloating** no unnecessary files, tests, or scaffolding.

Target: consensus among all reviewers that the result needs zero revisions.

## Progress Tracking

- Log work in `progress/session-NNN-brief-description.md` (literal `progress/` at repo root).
- Unified formatting; descriptive names for easy discovery.
- Keep `PLAN.md` current: remove completed or obsolete items after each session.

## Sub-agent Loop

1. Use sub-agents for parallel or specialized work.
1. After a sub-agent completes, have an **adversarial sub-agent** review it critically.
1. If recommendations exist, have another sub-agent evaluate and implement them.
1. Repeat until all sub-agents agree the result is exceptional with zero issues (minor or otherwise).
1. If not using sub-agents, apply this adversarial review loop on the main thread.
