# Session 221 — string ordering, format flags, const writes, next cursors

Issues: #199, #193, #130, #185.

## Changes

- Added `LuaStringOrdering.Compare`, which orders strings by Unicode code point so
  supplementary-plane characters sort after BMP characters like Lua's byte
  comparison. Routed relational operators, constant folding, and `table.sort`
  through it.
- `string.format` now accepts flags in any order (including `0`), parses empty
  precision (`%.g`), clamps string precision instead of aborting, applies integer
  `d`/`o`/`u` precision, emits C printf's two-digit minimum exponent, and matches
  Lua 5.4+ repeated/invalid-flag and character-precision rules per conversion
  type. The Lua 5.4 repeated-flag expectation in TAP `304-string.t` is
  version-aware.
- Lua 5.4/5.5 `<const>` and `<close>` locals and Lua 5.5 for-loop control
  variables now reject writes at compile time, including captured upvalues and
  `function name()` declaration syntax.
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
  `TableNextContractTUnitTests`), full TUnit suite `16088/16088` passes.
- Lua comparison: 5.1, 5.2, 5.3, and 5.5 corpus comparisons report zero
  mismatches and a clean error ratchet. Local Lua 5.4.4 differs from the pinned CI
  5.4.7 reference in two pre-existing fixtures (table.remove argument index and
  generic-for close ordering); both fixtures are unchanged by this work.
