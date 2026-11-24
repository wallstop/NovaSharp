# Session 191 — A5 table callback views

Phase A5 [#108](https://github.com/Ambiguous-Interactive/NovaSharp/issues/108)
moves each remaining table callback, including Lua 5.1 global `unpack`, to the
stack-only argument view. The old public CLR callback methods and their module
types leave the host API. The migration preserves Lua registration names
and version gates. The callbacks use the existing built-in delegate registration.
A method-level preservation attribute keeps the private entry points visible
to the Unity linker without a UnityEngine dependency.
Load is the next module in the Phase A5 queue.

The base was `877c0731`, the merged guidance PR
[#158](https://github.com/Ambiguous-Interactive/NovaSharp/pull/158). Its Tests,
CSharpier, and Benchmarks workflows succeeded on main. The paginated issue
inventory contained 29 open issues and the open PR list contained six tooling
bumps. Current tool pins suffice for the migration.

## Evidence

- The new registration test failed in all five Lua profiles on the base
  because the registered callbacks used legacy arguments. The test checks
  available functions through the Lua-visible table, including registration
  variants and the separately registered global. The first candidate run
  exposed a test bug: the raw pair enumeration returned a nil tombstone for
  the version-gated `move` entry. The test now enumerates non-nil entries and
  checks the version-gated names separately.
- The paired fixture exercises the registered functions across Lua 5.1–5.5,
  including range return counts, nil in a multi-return argument, and an
  overlapping move. Curating its version header ensures the corpus includes
  all five lanes. Automatic inference intersects function-version requirements
  across branches without evaluating their runtime version guards.
- The paired fixture found that Lua 5.1 and 5.2 `table.maxn` counted removed
  keys and skipped fractions. After removing a sequence entry, assigning key
  3.5, and removing it, stock Lua returned `2,3.5,2` on both versions.
  The base returned `3,2,2`. The scan now visits live entries and considers
  every positive numeric key.
- The Linux x64 Release interpreter built with the .NET 9.0.321 SDK. On each
  Lua profile, the same preloaded Lua loop accumulated the result of unpacking
  the single value 42 from a reused table 100,000 times. Five warmups followed
  by eleven samples, with tiered compilation disabled, yielded a median of
  312.00048 B per call on the base and 48.00048 B on the candidate. Every
  sample returned 4,200,000. Bytes include the Lua loop and the amortized
  allocation at the host entry point.
- Independent verification matched base and candidate outputs for all five
  profiles, including version exposure, trailing argument expansion, coercion,
  comparator reentrancy and failure, mutation, and range behavior. Its separate
  explicit-bound unpack probe measured the same 264 B reduction per call.
- The reference export inventory showed an existing difference in every
  version. [#159](https://github.com/Ambiguous-Interactive/NovaSharp/issues/159)
  records the CLR-derived names and the missing Lua 5.1 and 5.5 entries for a
  separate parity change. [#160](https://github.com/Ambiguous-Interactive/NovaSharp/issues/160)
  tracks two separate pre-existing `table.insert` divergences verified
  across all five Lua versions.
- The adversarial review identified the loss of Unity linker roots when the
  public callbacks became private. The replacement uses Unity's documented
  third-party attribute contract and preserves each of the nine entry points.
  [Unity PreserveAttribute](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Scripting.PreserveAttribute.html)

## Local checks

The quick build, 216 TableModule tests, 57 ModuleRegister tests, and all
15,721 quick tests passed with zero failures or skips. Regenerating the
corpus retained one comparable fixture in all five lanes.

Unity player execution was unavailable. Independent inspection of the
Release assembly confirmed nine method-level preservation annotations.

## Release note

Removed the public `TableModule` and `TableModuleGlobals` CLR callback surface.
The stack-only callback path reduces allocation on single-result
`unpack` calls. Lua 5.1 and 5.2 `table.maxn` now ignores removed entries
and includes fractional numeric keys.
