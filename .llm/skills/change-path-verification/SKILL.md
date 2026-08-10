---
name: change-path-verification
description: "Design red-to-green verification across every changed behavior and failure path. Use when mapping tests, edge cases, Lua-version coverage, or evidence for a code or CI change."
metadata:
  category: testing
  priority: core
  related: exhaustive-test-coverage, lua-comparison-harness, deterministic-testing
---
# Skill: Change-Path Verification

Line coverage is a backstop, not proof. Build a behavior-path map from the changed
entry point through downstream calls and effects.

## Path Map

For every changed condition or state transition, cover:

- nominal positive behavior;
- negative/absence behavior;
- invalid input and exact error behavior;
- lower/upper boundaries, zero, empty, nil/null, and extreme values;
- every guard, early return, fallback, exception, and cleanup path;
- repeated/idempotent use and state transitions;
- concurrency, reentrancy, cancellation, and resource ownership when applicable;
- all affected Lua versions and reference-Lua differential behavior;
- public host/interop and Unity/AOT boundaries;
- hot-path throughput and allocations when the path is performance-sensitive.

Mark an irrelevant dimension `N/A` with a reason. Never omit it silently.

## Red→Green Proof

For a bug fix:

1. Add or identify the smallest regression observation.
1. Run it on the base or known-bad behavior and record the expected failure.
1. Run the same observation on the fixed revision and record the pass.
1. Verify the expected result against each applicable reference Lua.
1. Add both required C# tests and `.lua` fixtures, then regenerate the corpus.

If the base revision cannot build or run, record the constraint and use a
controlled fault injection, differential output, or other falsifiable substitute.
A test that was only observed passing is green evidence, not red→green evidence.

## Review Table

| Claim | Decision/failure path | Test level | Oracle   | Versions/platforms | Evidence |
| ----- | --------------------- | ---------- | -------- | ------------------ | -------- |
| ...   | ...                   | Unit/diff  | Lua/spec | 5.1–5.5/...        | Command  |

Unmapped changed paths are actionable findings even when aggregate coverage
remains above its threshold.
