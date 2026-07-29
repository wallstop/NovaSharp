---
triggers:
  - "deterministic test"
  - "randomized test"
  - "replay seed"
  - "test ordering"
  - "timing dependent test"
category: testing
related:
  - exhaustive-test-coverage
  - test-failure-investigation
  - change-path-verification
priority: core
---
# Skill: Deterministic Testing and Replay

**When to use**: Tests involving randomness, time, ordering, concurrency,
localization, filesystem/environment state, or repeated execution.

## Control the Inputs

- Use an explicit seed; print it on failure and provide the exact replay command.
- Sort unordered inputs/outputs unless order is the behavior under test.
- Inject or bound clocks; do not depend on the wall clock or local timezone.
- Set culture/locale explicitly when parsing, formatting, or errors may vary.
- Isolate environment variables, working directories, static state, ports, and
  filesystem artifacts.
- Synchronize on state/events rather than sleeps.
- Avoid real networks in deterministic tests; model failures with controlled
  fakes. Bounded polling is allowed only for genuine external eventual
  consistency and must preserve diagnostics.

## Stress Without Hiding Failure

Use repetition and alternate scheduling/order as detectors, not as retry-to-pass:

1. run the targeted test with a fixed seed;
1. repeat concurrency/isolation scenarios enough to exercise scheduling;
1. vary culture/timezone or order only where relevant;
1. stop on the first failure and retain seed, iteration, input, revision, and
   environment;
1. replay the exact failure before diagnosing it.

Never discard a failed iteration because a later retry passed.

## Gate Receipt

Record seed, generator/version, iteration count, culture/timezone, relevant
environment controls, command, revision, and observed result. For Lua differential
fixtures also record the Lua executable/version and normalized comparison scope.

Randomized/property tests without replay data, timing tests using arbitrary
sleeps, and order-sensitive tests without declared ordering are not complete.
