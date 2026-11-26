# Session 221 — string ordering, format flags, const writes, next cursors

Issues: #193, #130, #185; advances #199 (byte-escape cases remain gated by #201).

## Changes

- Added `LuaStringOrdering.Compare`, which orders decoded source text by code
  point so supplementary-plane characters sort after BMP characters like Lua's
  UTF-8 byte comparison. Routed relational operators, constant folding, and
  `table.sort` through it. Raw byte-escape identity and ordering still require the
  representation work in #201, so #199 stays open.
- `string.format` now accepts flags in any order (including `0`), preserves the
  space sign under zero padding, parses empty precision (`%.g`), clamps string
  precision instead of aborting, applies integer `d`/`o`/`u`/`x` precision with
  C's zero-padding override and zero-value suppression, applies character width
  and alignment, emits C printf's two-digit minimum exponent, and matches Lua
  5.4+ repeated/invalid-flag and character-precision rules per conversion type.
  The Lua 5.4 repeated-flag expectation in TAP `304-string.t` is version-aware.
- Lua 5.4/5.5 `<const>` and `<close>` locals and Lua 5.5 for-loop control
  variables now reject writes at compile time, including captured upvalues and
  `function name()` declaration syntax. Diagnostics include the source location.
- Lua 5.3+ stores exact integral float table keys (including negative zero) as
  integer keys; Lua 5.4/5.5 reject the float spelling as a `next` cursor, and
  Lua 5.3 rejects it only for array-part keys, matching reference traversal keys
  and errors.

## Evidence

- Red: fixtures previously produced `false,false,false` for astral/BMP ordering,
  `%0+9g`/`%.g` literals, `1.500000e+000`, accepted const writes, and accepted
  `next(t, 2^53)` under Lua 5.5.
- Green: focused TUnit classes pass (`LuaStringOrderingTUnitTests`,
  `StringFormatFiniteTUnitTests`, `ForLoopConstVariableTUnitTests`,
  `TableNextContractTUnitTests`), full TUnit suite `16127/16127` passes. New
  fixtures print their compared outcomes so the corpus gate observes real output.
- Lua comparison: 5.1, 5.2, 5.3, and 5.5 corpus comparisons report zero
  mismatches and a clean error ratchet. Local Lua 5.4.4 differs from the pinned CI
  5.4.7 reference in two pre-existing fixtures (table.remove argument index and
  generic-for close ordering); both fixtures are unchanged by this work.
