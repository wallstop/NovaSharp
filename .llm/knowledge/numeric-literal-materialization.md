# Numeric Literal and Float Formatting Reference Matrix

Verified facts about reference Lua's numeral handling across 5.1-5.5, established
while fixing [#127](https://github.com/wallstop/NovaSharp/issues/127),
[#128](https://github.com/wallstop/NovaSharp/issues/128), and
[#132](https://github.com/wallstop/NovaSharp/issues/132). Sources: empirical probes
on the installed `lua5.1`-`lua5.5`, plus `lbaselib.c`/`lobject.c`/`luaconf.h` from
the official `lua-5.1.5`, `lua-5.2.4`, and `lua-5.5.0` tarballs. NovaSharp
implements this matrix in `LiteralExpression` → `LuaNumber.TryParse`,
`LuaNumber.ToLuaString`, and `BasicModule.ToNumber`.

## Source-literal materialization

- Lua 5.1/5.2 have one double number type: integer-syntax literals (decimal **and**
  hex) round to the nearest IEEE 754 double. `9007199254740993` prints
  `9.007199254741e+15` and equals `9007199254740992`.
- Lua 5.3+ keep integer subtypes; hex literals accumulate **modulo 2^64**
  (`0xffffffffffffffff` → `-1`, `0x10000000000000000` → `0`), and decimal literals
  beyond `lua_Integer` fall back to the float subtype (`99999999999999999999` →
  `1e+20` on every version).
- Lua 5.1 accepts hex *source literals* (`0x1A` → 26) even though stock 5.1 docs read
  as if hex arrived in 5.2; the lexer always scans `0x`, and `luaO_str2d`→strtod
  parses it. 5.1's lexer also consumes `p`-exponent hex literals through strtod
  (`0x1p4` → 16) but rejects `.`-forms and signed exponents (`0x1.5`, `0x.8`,
  `0x8p-3` are syntax errors); NovaSharp's lexer accepts all of them in every
  profile — a known pre-version-gating gap.

## Float→string (`tostring`, `print`, concat, `%s`)

- Lua 5.1-5.4 format floats with `%.14g` (`LUAI_NUMFFORMAT`): exponent form when the
  decimal exponent is `< -4` or `>= 14`, trailing zeros stripped, lowercase `e`,
  two-digit exponents (`1e-05`).
- Lua 5.5 (`LUA_NUMBER_FMT`/`LUA_NUMBER_FMT_N`, `tostringbuffFloat`) starts from
  `%.15g` and re-formats at `%.17g` when parsing the shorter digits back yields a
  different double: `1/3` → `0.33333333333333331`, but `0.1` → `0.1`.
- Lua 5.3+ append `.0` when the formatted result is all `[-0-9]`
  (`strspn(buff, "-0123456789")`): `2` → `2.0`, `-0` → `-0.0`, but `1e+15` stays
  `1e+15`. Lua 5.1/5.2 never append.
- .NET `G14`/`G15`/`G17` reproduce C `%g` digit-for-digit (including
  round-half-to-even ties like `1.2345678901234e+14`); only the exponent marker needs
  lowercasing (`Replace('E','e')`). The 5.5 round-trip check must compare double
  **bits**, not `==`, because `TryParse("-0")` must round-trip negative zero.
- **`io.write`/`f:write` do not use the tostring format**: Lua 5.1-5.4 print floats
  with plain `%.14g` — no `.0` suffix even on 5.3/5.4 (`io.write(2.0)` → `2`) — while
  Lua 5.5 routes through `lua_numbertocstring` (= tostring, `.0` included).
- Number→string coercion in argument validation (`luaL_checklstring`: `string.len`,
  `string.rep`, `table.concat` separator, `%s`, …) uses the **tostring** format of the
  running script's version; NovaSharp threads the owning script through
  `CallbackArguments.OwnerScript`/`CallbackArgumentsView` so `CheckTypeWithOwner`
  formats with it.

## `tonumber(v, base)`

- **Lua 5.1** (`luaB_tonumber`): base 10 — explicit or default — runs the *standard*
  conversion (`lua_isnumber`: `strtod`, so `'0x11'`→17, `'3.14'`→3.14). Other bases
  defer to `strtoul`: leading spaces skipped, optional sign applied via **unsigned
  wraparound** (`'-ff'` base 16 → `1.844674407371e+19`), optional `0x` prefix
  accepted **in base 16**, overflow **saturates** at `ULONG_MAX` (17 f's →
  `1.844674407371e+19`), trailing spaces allowed, any other trailing character → nil.
- **Lua 5.2**: strips spaces/sign, accumulates `n = n*base + digit` in **double**
  (arbitrary magnitude, `2^68-1`-exact), **signed** negation (`'-ff'` → `-255`), no
  0x prefix (`'0x11'` base 16 → nil), full-string consumption required.
- **Lua 5.3+**: accumulate **modulo 2^64** into the integer subtype
  (`'ffffffffffffffff'` → `-1`), sign via two's complement, full-string, no 0x
  prefix; **number arguments are rejected** (`bad argument #1 ... string expected, got number`).
- The **base argument** is version-sensitive too: Lua 5.1/5.2 truncate a fractional
  base (`tonumber('12', 3.5)` → 5) and report NaN/Infinity as `base out of range`;
  Lua 5.3+ reject any non-integer-representable base with
  `number has no integer representation`.
- 5.1/5.2 coerce *number* arguments to strings with the version's `tostring`
  formatting before parsing (`luaL_checkstring`), so `tonumber(111, 2)` → 7 —
  `tonumber(2^53, 2)` → nil because the coerced `"9007199254740992"` is invalid
  base-2.
- Full-string validation in every version: `'7g'` base 16, `'17'` base 6, `'09'`
  base 8 → nil (5.1's C `strtoul` would accept prefixes; the trailing-character
  check rejects them).

## Traps verified along the way

- `for i = 9007199254740990, ...` **hangs in reference 5.1/5.2** (ulp stagnation at
  2^53); do not "fix" NovaSharp looping there without checking reference.
- Buffered writers over `Console.OpenStandardOutput()` lose the final partial block
  at process exit — standard IO handles must `AutoFlush` (nothing flushes them at
  host exit; C's runtime flushes stdout at exit, which is why reference behaves
  differently).
- The corpus extractor emits *comparable* snippets for any resolvable embedded Lua
  literal: partial fragments like `"return tostring(" + x` extract as a broken
  runnable file. Route interpolated test code through an unresolved placeholder
  (`"return tostring({expression})"`) so the derived snippet is marked
  NovaSharp-only, and put the reference-verified copy in `LuaFixtures/`.
- Unversioned number→string conversion (`CastToString()`/`LuaNumber.ToString()`)
  defaults to Lua 5.3 formatting; every user-visible path (concat, `%s`, error text)
  must thread the script's version or 5.1/5.2 print `123.0` for `123`.
