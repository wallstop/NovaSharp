# Session 148: A1b Tuple Fixtures and Table Constructor Borders

Date: 2026-07-04

## Summary

- Added A1b regression coverage for `null`/`Nil`/`Void` drift hazards before the `LuaValue` struct conversion.
- Covered `select('#', ...)`, nil-in-middle multiple returns, function-call expression-list adjustment, statement-position argument expansion, parenthesized scalarization, and `table.pack(...).n` with expanded nil values.
- Added standalone Lua fixtures with assertions so the comparison harness catches arity and nil-preservation regressions, not just process success.
- Fixed version-specific `#` behavior for constructor-created holey arrays by preserving original constructor array-field borders across same-slot writes while clearing them for unrelated mutations.
- Matched Lua 5.1-5.3 constructor-array binary-search borders, Lua 5.4 highest constructor border behavior, and Lua 5.5 prefix behavior for the covered constructor cases.
- Addressed adversarial review findings by preserving constructor-border hints for original-slot writes and mixed keyed/numeric constructor fields, matching Lua 5.4 absent string/value-key nil no-op behavior, clearing and invalidating cached length for real string/value-key mutations, restoring script ownership validation for constructor array fields, adding Lua-side assertions to the inline TUnit snippets, adding non-final function-call scalarization coverage, and anchoring fixture metadata to source lines.
- Final self-review kept constructor-specific numeric keyed writes inside table-constructor initialization while leaving ordinary post-construction numeric writes on the normal invalidation path.

## Validation

- Reference Lua fixture checks passed for the new A1b fixtures on Lua 5.1, 5.2, 5.3, 5.4, and 5.5, with the `table.pack` fixture limited to Lua 5.2+.
- Reference Lua fixture checks passed for `TableLengthFollowsVersionedConstructorBorders.lua` on Lua 5.1, 5.2, 5.3, 5.4, and 5.5.
- `./scripts/test/quick.sh --full SelectHashCountsExpandedNilReturnValues` passed: 5 tests, 0 failures.
- `./scripts/test/quick.sh FunctionCallExpressionPositionsAdjustReturnArity` passed: 5 tests, 0 failures.
- `./scripts/test/quick.sh PackPreservesExpandedNilAndReportsCount` passed: 4 tests, 0 failures.
- `./scripts/test/quick.sh --full TableLengthFollowsVersionedConstructorBorders` passed after mutation cases were added: 165 tests, 0 failures.
- `./scripts/test/quick.sh ArrayConstructorRejectsForeignScriptResource` passed: 5 tests, 0 failures.
- Scoped comparison harness over the four new fixtures passed with `--enforce --skip-error-ratchet` for Lua 5.1, 5.2, 5.3, 5.4, and 5.5.
- `./scripts/build/quick.sh` passed.
- Targeted class suites passed: `./scripts/test/quick.sh -c TableTUnitTests` (605 tests), `./scripts/test/quick.sh -c TableModuleTUnitTests` (211 tests), and `./scripts/test/quick.sh -c SimpleTUnitTests` (417 tests).
- Full TUnit suite passed: `./scripts/test/quick.sh` (14,775 tests, 0 failures).
- Full Lua fixture comparison passed with `--enforce` for Lua 5.1, 5.2, 5.3, 5.4, and 5.5 with 0 mismatches and 0 missing outputs.
- `bash ./scripts/dev/pre-commit.sh` completed successfully; it reported existing documentation and skill metadata warnings only.

## Notes

- A full fixture corpus regeneration currently rewrites broad unrelated fixture metadata and emits many untracked snippets, so this session kept the new fixtures scoped manually and did not commit unrelated generated churn.
- Broader non-constructor mutation border behavior remains part of the planned A4 table rewrite; this session fixes the constructor-created hole class that was immediately blocking stronger pre-A4 coverage.
