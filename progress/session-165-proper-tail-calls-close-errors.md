# Session 165: Proper Tail Calls and Close Errors

Date: 2026-07-06

## Summary

- Advanced Phase A5 by implementing immediate Lua tail-call frame reuse for legal `return f(...)` calls while preserving caller frames when `<close>` variables, continuations, or error handlers make reuse invalid.
- Added Lua 5.2+ `debug.getinfo(..., "t")` support for `istailcall` and covered tail-call frame reporting, callable table targets, pcall/xpcall targets, sandbox call-depth behavior, and Lua 5.4+ `<close>` exclusions.
- Removed the fixed VM value-stack ceiling by making `FastStack` grow geometrically.
- Hardened Lua 5.4+ unwind behavior so `xpcall` message handlers run before scope close handlers, close-handler replacement errors are decorated by the correct active handler, remaining close handlers still run after a close error, and recursive handler failures do not re-enter the same handler.
- Added standalone Lua fixtures for the new tail-call and error-unwind coverage.

## Review Loop

- A first adversarial review found a Lua 5.4 `xpcall` + `<close>` bug where a throwing close handler bypassed the active `xpcall` message handler.
- A second adversarial review found recursive message-handler re-entry and nested `xpcall` handler-boundary bugs.
- A final adversarial review reran focused tests and independent probes and reported no remaining issues.

## Validation

- `./scripts/test/quick.sh --full -c ErrorHandlingModuleTUnitTests` completed with exit code 0: 166 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c TailCallTUnitTests` completed with exit code 0: 112 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c CloseAttributeTUnitTests` completed with exit code 0: 14 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c ProcessorCoroutineCloseTUnitTests` completed with exit code 0: 58 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c ProcessorStackTraceTUnitTests` completed with exit code 0: 20 tests passed, 0 failed.
- `./scripts/test/quick.sh --full -c FastStackTUnitTests` completed with exit code 0: 13 tests passed, 0 failed.
- `./scripts/build/quick.sh` completed with exit code 0.
- `./scripts/test/quick.sh` completed with exit code 0: 15,001 tests passed, 0 failed, 0 skipped.
- Reference Lua fixture probes for the new fixtures matched installed Lua 5.1-5.5 versions where applicable.
- `scripts/tests/run-lua-fixtures-fast.sh` plus `scripts/tests/compare-lua-outputs.py --enforce` completed with exit code 0 for Lua 5.1, 5.2, 5.3, 5.4, and 5.5: zero mismatches and no new or changed both-error ratchet entries.
- `git diff --cached --check` completed with exit code 0.
- `python3 scripts/ci/format_markdown.py --check --files PLAN.md progress/session-165-proper-tail-calls-close-errors.md` completed with exit code 0.
- `bash ./scripts/dev/pre-commit.sh` completed with exit code 0 with existing LLM skill metadata warnings.
- PR CI and reviewer feedback are still pending.
