# Operator metamethod results and yield boundaries

The delivery starts from `1e1345e8`. GitHub listed 59 open issues and no open
pull requests, including dependency updates and drafts. Tests, Benchmarks,
and CSharpier succeeded on the base revision. This slice groups three related
operator dispatch issues that affect Lua scripts.

- [#213](https://github.com/Ambiguous-Interactive/NovaSharp/issues/213)
  A Lua 5.1 operator hook could suspend when a nested Lua call reached
  `coroutine.yield`. The VM now carries the operator boundary through tail
  calls and checks it before unwinding an explicit yield. The probes cover
  protected recovery, subsequent legitimate yields, host callbacks, and
  instruction-budget and host-suspension controls.
- [#214](https://github.com/Ambiguous-Interactive/NovaSharp/issues/214)
  Stock Lua makes a metatable set on a closure visible to native callbacks.
  The base failed that check in all five profiles. Both representations now
  use the same script-local metatable, including for debugger access and
  operator dispatch.
- [#215](https://github.com/Ambiguous-Interactive/NovaSharp/issues/215)
  Stock Lua returned one value from unary hooks whose bodies returned three
  or zero. The base returned three or zero in all five profiles. Operator
  dispatch now takes the first result through normal, tail, and suspended
  calls, with nil for an empty result. The direct-yield probes also exposed
  arithmetic and concatenation call sites that treated suspension as a
  missing hook. All affected call sites distinguish the two outcomes.

Two failures reproduced on the base through ordinary calls and remain queued.
[Native protected-call yields](https://github.com/Ambiguous-Interactive/NovaSharp/issues/217)
are rejected even in yieldable profiles, and
[CLR tail-call error cleanup](https://github.com/Ambiguous-Interactive/NovaSharp/issues/218)
can leave the throwing callback on the stack after recovery.

## Validation and reflection

The focused suites passed 25 yield boundary, 14 operator result, five function
metatable, five debug descriptor dispatch, and three script metatable cases.
The interpreter and solution quick builds reported zero warnings and errors.
The full quick suite passed 16,019 tests. The pre-commit gate passed.

Independent verification matched 958 reference results across the five
profiles and checked 1,000 mixed operator and ordinary-call coroutine cycles.
The enforced corpus comparison checked 6,199 fixture/profile combinations
with PUC Lua 5.1.5, 5.2.4, 5.3.6, 5.4.7, and 5.5.0. Two Lua 5.1 file-load
diagnostics were replayed from a recognized checkout directory because the
worktree path affects their signatures.
[#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165)

The corpus also exposed a CLR-hosted sample marked as portable. The TUnit
test supplies a registered CLR object and passed in all five profiles.
The regenerated manifest now classifies the sample as NovaSharp-only, and
the ratchet removes only its five old error entries. The existing extractor
issue tracks the larger classification problem.
[#99](https://github.com/Ambiguous-Interactive/NovaSharp/issues/99)

The paired fixtures and metadata correction capture the verified findings.
The investigation found no need for another repository guidance change.
