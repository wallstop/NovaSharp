---
name: architecture-review
description: "Review a material NovaSharp design or plan through scope, invariants, testability, performance, memory, and Unity gates. Use before architecture, public API, VM/compiler, storage, CI, or cross-cutting changes."
metadata:
  category: workflow
  priority: recommended
  related: correctness-then-performance, high-performance-csharp, exhaustive-test-coverage
---
# Skill: Architecture and Plan Review

## Four Gates

### 1. Scope and Simplicity

- State the user-visible problem and acceptance criteria without prescribing the
  solution.
- Search for an existing mechanism to delete, repair, or extend.
- Compare at least two viable approaches when the choice is consequential.
- Prefer boring, reversible designs with fewer states and authorities.
- Name out-of-scope work and a rollback/containment path.

### 2. Invariants, Flow, and Failure

- Identify sources of truth, ownership, lifetimes, and mutation points.
- Trace inputs through transformations to outputs and side effects.
- Map state transitions, early returns, exceptions, cancellation, cleanup, and
  partial failure.
- Use a small diagram only when it exposes relationships prose would hide.
- State Lua-version and platform differences explicitly.

### 3. Testability and Observability

- Define the red observation before implementation.
- Map each decision/failure path to a unit, integration, differential, or
  performance check.
- Make time, randomness, environment, culture, and scheduling controllable where
  relevant.
- Specify diagnostics that preserve the failing input, seed, revision, and stage.

### 4. Performance, Memory, and Unity

- Identify hot paths and allocation budgets before adding abstraction.
- Define paired base/head measurement, workload, warmup, iterations, environment,
  and noise threshold for performance claims.
- Review IL2CPP/AOT, Mono, and `netstandard2.1` constraints.
- Reject optimization that changes reference-Lua behavior.

## Approval Contract

Return `APPROVE` only when all gates have evidence or an explicit `N/A` rationale,
no unresolved high-impact assumption remains, and implementation steps are small
enough to verify independently. Otherwise return `REQUEST_CHANGES` with the
missing decision, evidence, or test—not a numeric quality score.
