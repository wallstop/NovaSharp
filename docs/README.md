# Documentation Index

This folder hosts the canonical documentation set for NovaSharp. Use the index below to find the right guide for your task.

## Quick Links

| Topic                          | Path                                                            | Notes                                                                                                         |
| ------------------------------ | --------------------------------------------------------------- | ------------------------------------------------------------------------------------------------------------- |
| Testing / CI workflow          | `docs/Testing.md`                                               | NUnit layout, TAP fixtures, coverage workflow (`scripts/coverage/coverage.ps1` / `.sh`).                      |
| Coverage dashboards & hotspots | `docs/coverage/README.md`, `docs/coverage/coverage-hotspots.md` | Explains generated artefacts, summarizes modules below target coverage, and links to the latest HTML reports. |
| Lua compatibility matrix       | `docs/LuaCompatibility.md`                                      | Tracks Lua 5.4.8 parity plus work-in-progress items with owners.                                              |
| Modernization notes            | `docs/Modernization.md`, `docs/modernization/*`                 | Architecture decisions, legacy cleanup plans, and audits (vestigial code, reflection usage, branding).        |
| Project structure blueprint    | `docs/ProjectStructureBlueprint.md`                             | Current vs. proposed directory/solution layout with migration phases.                                         |
| Performance benchmarks         | `docs/Performance.md`, `docs/performance-history/`              | Benchmark harness guidance and historical runs.                                                               |
| Unity integration              | `docs/UnityIntegration.md`                                      | Unity-specific packaging, IL2CPP guidance, and sample workflows.                                              |
| Proposals                      | `docs/proposals/`                                               | Roslyn hardwire generator, namespace rules, and upcoming feature designs.                                     |
| Script tooling                 | `scripts/README.md`, `scripts/coverage/README.md`               | Entry point for helper scripts (build, coverage, branding guards, etc.).                                      |

## Contributing Expectations

1. **Update the index** whenever you add a new Markdown guide. Link it from the table above (or add a new section) so discoverability stays high.
1. **Document scripts**: if you add a script under `scripts/`, extend `scripts/README.md` (and the relevant subfolder README) plus any guide that references the workflow (most commonly `docs/Testing.md` or `docs/Modernization.md`).
1. **Cross-link related docs**: for example, when editing coverage processes, update this index, `docs/Testing.md`, and the relevant plan entries so readers land on current instructions quickly.

Keeping the docs organized is a first-class deliverable—treat documentation edits like code changes by running spell checks, verifying links, and previewing Markdown before submitting.
