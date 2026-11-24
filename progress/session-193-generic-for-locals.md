# Session 193 — Generic-for locals and closing values

The [generic-for parity change](https://github.com/Ambiguous-Interactive/NovaSharp/issues/164)
evaluates a nil initializer for every bare local declaration. The previous
compiler emitted stores without an initializer on the value stack. In a
generic loop the stores consequently read the iterator tuple's state and
control entries.

Lua 5.4 and 5.5 assign the fourth iterator expression result to a hidden
to-be-closed local. The compiler gives it a scope for the whole loop and
reuses the existing close machinery. Breaks emit exits for each crossed
lexical scope so a nested break closes body locals before the iterator.
Callable values can serve as close metamethods, and file userdata supplies
an idempotent closer for `io.lines(filename)`. The iterator also closes
the file when a direct caller reaches EOF.
The [Lua 5.4 parser](https://www.lua.org/source/5.4/lparser.c.html#forlist)
places the closing value outside the iteration block. The
[Lua 5.4.4 erratum](https://www.lua.org/bugs.html#5.4.4-6) informed the
choice of Lua 5.4.9 for the nested-close oracle.

## Evidence

- Main at `4952361d` had successful Tests, CSharpier, Benchmarks, and Pages
  runs. There were no open PRs, including drafts and dependency updates.
- The first focused test run on `4952361d` failed the nil test in all five
  profiles and the closing-value test in Lua 5.4 and 5.5. Both failures
  disappeared after the compiler changes.
- The regression covers multiple declarations, repeated and nested iteration,
  captured locals, untouched iterator state and control, explicit and
  zero-result initializers, and numeric loops as a control.
- Fourth-value coverage includes zero iterations, exhaustion, nested break,
  return, goto, body and iterator errors, false, a rejected non-closable value,
  and an ignored fifth value. The dispatch test covers callable and replaced
  metamethods and closing a suspended coroutine. Standalone fixtures passed
  the applicable PUC profiles. Nested close ordering passed Lua 5.4.7,
  5.4.9, and 5.5.0.
- The first full run found seven failures because the fourth result of
  `io.lines(filename)` had no close metamethod. The paired IO regression
  reproduced the non-closable file error and passed after descriptor-aware
  closing. The 31 focused IO cases passed. Five later failures exposed a
  reflected `file:close()` regression. Keeping a public parameterless
  wrapper restored that entry point, and all 82 focused file API cases
  passed.
- Independent review reproduced another integration failure. Lua 5.4
  `pairs` discards a fourth hook result, but NovaSharp forwarded it into
  the now-active closing slot. Lua 5.5 uses four results. Lua 5.4 and 5.5
  permit iterator hooks to yield, including native callbacks and callable
  tables. Stock Lua 5.3's build flags enable `__ipairs`, as confirmed by
  rebuilding the official archive and running the probe. The
  [Lua 5.4 base library](https://www.lua.org/source/5.4/lbaselib.c.html#luaB_pairs)
  and [Lua 5.5 base library](https://www.lua.org/source/5.5/lbaselib.c.html#luaB_pairs)
  explain the difference in result count. A small cached bytecode frame
  calls each hook and normalizes its results, preserving native and callable
  yields as well as direct protected calls. A follow-up probe verified the
  nil result still occupies one argument slot.
- The quick build, 53 value-slot cases, 19 iterator cases, all 15,777
  quick tests, CSharpier, and pre-commit passed. The Docker-only pre-commit check was
  unavailable. The new compiler instructions use existing opcodes and
  covered runtime paths. Unity player execution was unavailable.
- Independent verification passed 30 PUC comparisons and four bound CLR
  hook cases. A separate file API review verified the reflected close path.
- All five enforced corpus comparisons passed from the primary checkout
  using candidate fixtures and binaries. Running from the temporary worktree
  first reproduced the two file-load error signature changes already tracked
  in [#165](https://github.com/Ambiguous-Interactive/NovaSharp/issues/165).

## Hosted review

- The callable closer already worked when the final target was Lua. A native
  callback reached through the same chain observed the main coroutine.
  Resolving and invoking the chain in the active processor fixed the native
  path. Eight independent cases covered both callback styles in Lua 5.4–5.5.
- The filename iterator now has the reference function type and exact result
  count. It returns no values at EOF and raises a closed-file error on a
  subsequent call. This closes [#170](https://github.com/Ambiguous-Interactive/NovaSharp/issues/170).
- Stack inspection now exposes the original C iterator callback for both
  synchronous and resumable hooks. The paired regression covers stack depth,
  function identity and hidden locals after aliasing, global reassignment and
  protected calls.
- CodeRabbit reproduced a nested host call re-enabling yield in an outer
  Lua 5.2 or 5.3 iterator hook. Restoring the prior yield permission fixes
  both host call entry points. The same nested Lua probe still yields as
  expected in Lua 5.4 and 5.5.
- The default `ipairs` generator now uses the first indexed result and
  terminates when the result is absent or nil. The reference probe used an
  indexed value followed by a zero-result lookup. NovaSharp previously
  supplied an extra nil loop value. This closes [#171](https://github.com/Ambiguous-Interactive/NovaSharp/issues/171).
- The review revision passed 30 iterator, 16 generic-for, 692 debugger,
  and 31 filename iterator cases. The quick build, all 15,790 quick tests,
  CSharpier, pre-commit, and the five enforced reference comparators passed.

## Issue triage

The initial two-page inventory contained 35 open issues. Gameplay and Lua
caller order was 164, 167, 162, 130, 125, 159, 138, 124, and 122. Runtime order
was 108, 95, 93, 92, 118, 105, 87, 88, 104, and 103. Tooling and maintenance
order was 113, 114, 135, 119, 94, 165, 154, 98, 99, 106, 84, 102, 85, 121, 163,
and 157.

[Issue 168](https://github.com/Ambiguous-Interactive/NovaSharp/issues/168)
tracks the loss of Lua error values in the shared protected-call and
close-unwinding paths. Its scope includes explicit close locals as well as
generic-for closers.

[Issue 169](https://github.com/Ambiguous-Interactive/NovaSharp/issues/169)
tracks normal-exit closers that suspend a coroutine.

[Issue 172](https://github.com/Ambiguous-Interactive/NovaSharp/issues/172)
tracks the existing case where an explicit initializer returns no values and
a subsequent argument omits the resulting local instead of passing nil.

[Issue 174](https://github.com/Ambiguous-Interactive/NovaSharp/issues/174)
records a pre-existing difference in when closers run after an uncaught
coroutine error.

[Issue 175](https://github.com/Ambiguous-Interactive/NovaSharp/issues/175)
tracks borrowed line iterator results and optional read format handling.
