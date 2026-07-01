# Session 110 - Hardwire Registry Test Isolation

## Baseline

- Branch: `dev/wallstop/api-perf`
- Related work: `progress/session-109-nested-global-function-binding.md`
- Trigger: the post-format full test pass for session 109 failed in `DiscoverFromAssemblyRegistersGenerators`.
- Failure: the test expected `DynValueMemberDescriptorGenerator` but resolved `NullGenerator`.

## Assessment

- This was a test isolation issue, not a production Lua behavior issue.
- `HardwireGeneratorRegistry` is a process-wide static registry.
- `HardwireGeneratorTUnitTests` resets and repopulates that registry under a private class-local gate.
- Other hardwire registry, descriptor, CLI, and integration tests could still run in parallel and observe the registry after a reset but before repopulation.
- Production registry operations already lock individual reads/writes; the failing signal was the test suite running shared-static-state tests concurrently.

## Changes

1. Added `[NotInParallel(nameof(HardwireGeneratorRegistry))]` to direct hardwire registry tests and hardwire generation paths that depend on the registry.
2. Applied the same keyed isolation to CLI and integration tests that invoke hardwire generation through `HardwireCommand`, `Program.CheckArgs`, or the REPL hardwire command.
3. Converted `RegisterPredefinedPopulatesBuiltInGenerators` to a small data-driven test covering both `DynValueMemberDescriptorGenerator` and `MethodMemberDescriptorGenerator`.
4. Added assertion diagnostics that report the requested managed type and resolved generator type when registry discovery fails.

## Validation

- `./scripts/test/quick.sh --full DiscoverFromAssembly`: passed, 1 test.
- `./scripts/test/quick.sh RegisterPredefinedPopulatesBuiltInGenerators`: passed, 2 tests.
- `./scripts/test/quick.sh`: passed, 14,373 tests.
- Final post-pre-commit `dotnet build src/NovaSharp.sln -c Release --no-restore`: passed with 0 warnings and 0 errors.
- Final post-pre-commit `./scripts/test/quick.sh`: passed, 14,373 tests.

## Status

Ready to include with the nested global function binding commit.
