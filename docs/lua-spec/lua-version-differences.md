# Lua Version Behavioral Differences: 5.1, 5.2, 5.3, 5.4

> **Purpose**: Comprehensive reference for auditing NovaSharp interpreter version compatibility
> **Last Updated**: 2025-12-11
> **Sources**: Official Lua Reference Manuals for each version

______________________________________________________________________

## Table of Contents

1. [Summary Matrix](#1-summary-matrix)
1. [math.random and math.randomseed](#2-mathrandom-and-mathrandomseed)
1. [Integer vs Float Types](#3-integer-vs-float-types)
1. [String Library Changes](#4-string-library-changes)
1. [Table Library Changes](#5-table-library-changes)
1. [Bitwise Operations](#6-bitwise-operations)
1. [Coroutine Library Changes](#7-coroutine-library-changes)
1. [Basic Functions](#8-basic-functions)
1. [Module System Changes](#9-module-system-changes)
1. [Metamethod Changes](#10-metamethod-changes)
1. [String-to-Number Coercion](#11-string-to-number-coercion)
1. [Implementation Priority](#12-implementation-priority)

______________________________________________________________________

## 1. Summary Matrix

| Feature                | 5.1                 | 5.2                        | 5.3                    | 5.4                    |
| ---------------------- | ------------------- | -------------------------- | ---------------------- | ---------------------- |
| **Number Type**        | Float only          | Float only                 | Integer + Float        | Integer + Float        |
| **Bitwise Ops**        | ❌ None             | `bit32` library            | Native operators       | Native operators       |
| **Floor Division**     | ❌ None             | ❌ None                    | `//` operator          | `//` operator          |
| **Environment**        | `setfenv`/`getfenv` | `_ENV` mechanism           | `_ENV` mechanism       | `_ENV` mechanism       |
| **Goto/Labels**        | ❌ None             | ✅ `::label::`, `goto`     | ✅ `::label::`, `goto` | ✅ `::label::`, `goto` |
| **RNG Algorithm**      | Platform C library  | Platform C library         | Platform C library     | xoshiro256\*\*         |
| **Coroutine Yield**    | Not from pcall      | ✅ Yieldable pcall         | ✅ Yieldable pcall     | ✅ Yieldable pcall     |
| **To-be-closed**       | ❌ None             | ❌ None                    | ❌ None                | ✅ `local <close>`     |
| **Const variables**    | ❌ None             | ❌ None                    | ❌ None                | ✅ `local <const>`     |
| **warn()**             | ❌ None             | ❌ None                    | ❌ None                | ✅ Available           |
| **UTF-8 Library**      | ❌ None             | ❌ None                    | ✅ `utf8.*`            | ✅ `utf8.*`            |
| **String Pack/Unpack** | ❌ None             | ❌ None                    | ✅ Available           | ✅ Available           |
| **GC Mode**            | Incremental         | Incremental + Generational | Incremental            | Generational (default) |

______________________________________________________________________

## 2. math.random and math.randomseed

### 2.1 math.random() Signatures

| Version | No args       | Single arg `n`  | Two args `m, n` | `math.random(0)`      |
| ------- | ------------- | --------------- | --------------- | --------------------- |
| 5.1     | `[0,1)` float | `[1,n]` integer | `[m,n]` integer | ❌ N/A                |
| 5.2     | `[0,1)` float | `[1,n]` integer | `[m,n]` integer | ❌ N/A                |
| 5.3     | `[0,1)` float | `[1,n]` integer | `[m,n]` integer | ❌ N/A                |
| 5.4     | `[0,1)` float | `[1,n]` integer | `[m,n]` integer | ✅ All 64 bits random |

### 2.2 math.randomseed() Changes

| Version | Signature                    | Return Value   | Notes                        |
| ------- | ---------------------------- | -------------- | ---------------------------- |
| 5.1     | `math.randomseed(x)`         | None           | Uses platform RNG            |
| 5.2     | `math.randomseed(x)`         | None           | Uses platform RNG            |
| 5.3     | `math.randomseed(x)`         | None           | Uses platform RNG            |
| 5.4     | `math.randomseed([x [, y]])` | Returns `x, y` | 128-bit seed, xoshiro256\*\* |

### 2.3 RNG Algorithm Differences

**Lua 5.1-5.3**: Uses the C standard library's `rand()` function. The quality and period depend entirely on the platform implementation.

**Lua 5.4**: Uses **xoshiro256**\*\* algorithm:

- Better statistical properties
- 256-bit state (128-bit seed via two integers)
- Random default seed each run (sequences differ)
- Reproducible with explicit seed

```lua
-- Lua 5.4: Get and restore seed
local x, y = math.randomseed()  -- Returns seed used
math.randomseed(x, y)           -- Reproduce sequence

-- Lua 5.4: All bits random
local r = math.random(0)  -- Returns 64-bit random integer
```

### 2.4 Implementation Notes for NovaSharp

- **5.1-5.3**: Can use .NET `Random` class or similar
- **5.4**: Should implement xoshiro256\*\* for spec compliance
- **Seed handling**: 5.4 requires tracking two seed components

______________________________________________________________________

## 3. Integer vs Float Types

### 3.1 Type System Changes

| Version | Number Subtypes                   | `type()` result | `math.type()`       |
| ------- | --------------------------------- | --------------- | ------------------- |
| 5.1     | Float only (double)               | "number"        | ❌ N/A              |
| 5.2     | Float only (double)               | "number"        | ❌ N/A              |
| 5.3     | Integer (64-bit) + Float (double) | "number"        | "integer" / "float" |
| 5.4     | Integer (64-bit) + Float (double) | "number"        | "integer" / "float" |

### 3.2 Integer-Specific Constants (5.3+)

```lua
math.maxinteger  -- 9223372036854775807 (2^63 - 1)
math.mininteger  -- -9223372036854775808 (-2^63)
```

### 3.3 Arithmetic Operation Results (5.3+)

| Operation     | Operands      | Result Type  |
| ------------- | ------------- | ------------ |
| `+`, `-`, `*` | Both integers | Integer      |
| `+`, `-`, `*` | Any float     | Float        |
| `/`           | Any           | Always float |
| `//`          | Both integers | Integer      |
| `//`          | Any float     | Float        |
| `%`           | Both integers | Integer      |
| `%`           | Any float     | Float        |
| `^`           | Any           | Always float |
| Bitwise ops   | Any           | Integer      |

### 3.4 Integer Overflow Behavior (5.3+)

```lua
math.maxinteger + 1 == math.mininteger  -- true (wraps)
math.mininteger - 1 == math.maxinteger  -- true (wraps)
```

### 3.5 Floor/Ceil Return Types (5.3+)

| Version | `math.floor(3.5)`      | `math.ceil(3.5)`       |
| ------- | ---------------------- | ---------------------- |
| 5.1-5.2 | `3.0` (float)          | `4.0` (float)          |
| 5.3-5.4 | `3` (integer, if fits) | `4` (integer, if fits) |

### 3.6 Conversion Functions (5.3+)

```lua
-- New in 5.3
math.type(3)        -- "integer"
math.type(3.0)      -- "float"
math.type("3")      -- nil (not a number)

math.tointeger(3.0) -- 3 (exact conversion)
math.tointeger(3.5) -- nil (not exact)

math.ult(m, n)      -- Unsigned less-than comparison
```

### 3.7 Float Printing (5.3+)

Floats that look like integers print with `.0` suffix:

```lua
-- 5.1-5.2
tostring(42.0)  --> "42"

-- 5.3+
tostring(42.0)  --> "42.0"
```

______________________________________________________________________

## 4. String Library Changes

### 4.1 string.format %q Specifier

| Version | Input Types                     | Behavior                            |
| ------- | ------------------------------- | ----------------------------------- |
| 5.1-5.3 | Strings only                    | Quotes string with escape sequences |
| 5.4     | Strings, booleans, nil, numbers | Handles all types                   |

```lua
-- 5.4 only
string.format("%q", true)   --> "true"
string.format("%q", nil)    --> "nil"
string.format("%q", 42)     --> "42"
```

### 4.2 string.format %p Specifier (5.4+)

```lua
-- New in 5.4
string.format("%p", {})     --> "0x..." (pointer address)
```

### 4.3 string.gmatch init Parameter

| Version | Signature                            | Notes                   |
| ------- | ------------------------------------ | ----------------------- |
| 5.1-5.3 | `string.gmatch(s, pattern)`          | No init parameter       |
| 5.4     | `string.gmatch(s, pattern [, init])` | Optional start position |

### 4.4 string.dump strip Parameter (5.3+)

```lua
-- 5.1-5.2
string.dump(f)  -- Always includes debug info

-- 5.3+
string.dump(f [, strip])  -- strip=true removes debug info
```

### 4.5 string.rep Separator (5.2+)

```lua
-- 5.1
string.rep("ab", 3)       --> "ababab"

-- 5.2+
string.rep("ab", 3, "-")  --> "ab-ab-ab"
```

### 4.6 Escape Sequences

| Escape     | 5.1 | 5.2+       | Description       |
| ---------- | --- | ---------- | ----------------- |
| `\xHH`     | ❌  | ✅         | Hexadecimal byte  |
| `\z`       | ❌  | ✅         | Skip whitespace   |
| `\u{XXXX}` | ❌  | ❌/✅ 5.3+ | Unicode codepoint |

### 4.7 String Pack/Unpack (5.3+)

```lua
-- New in 5.3
string.pack(fmt, v1, v2, ...)      -- Pack to binary
string.unpack(fmt, s [, pos])      -- Unpack from binary
string.packsize(fmt)               -- Get format size
```

______________________________________________________________________

## 5. Table Library Changes

### 5.1 Function Availability

| Function          | 5.1 | 5.2        | 5.3 | 5.4 |
| ----------------- | --- | ---------- | --- | --- |
| `table.concat`    | ✅  | ✅         | ✅  | ✅  |
| `table.insert`    | ✅  | ✅         | ✅  | ✅  |
| `table.remove`    | ✅  | ✅         | ✅  | ✅  |
| `table.sort`      | ✅  | ✅         | ✅  | ✅  |
| `table.maxn`      | ✅  | ❌ Removed | ❌  | ❌  |
| `unpack` (global) | ✅  | ❌ Moved   | ❌  | ❌  |
| `table.unpack`    | ❌  | ✅         | ✅  | ✅  |
| `table.pack`      | ❌  | ✅         | ✅  | ✅  |
| `table.move`      | ❌  | ❌         | ✅  | ✅  |

### 5.2 table.pack (5.2+)

```lua
local t = table.pack(1, 2, 3)
-- t = {1, 2, 3, n = 3}
-- The 'n' field is important for handling nils
```

### 5.3 table.unpack vs unpack

```lua
-- 5.1
a, b, c = unpack({1, 2, 3})

-- 5.2+
a, b, c = table.unpack({1, 2, 3})
-- 'unpack' is no longer a global function
```

### 5.4 table.move (5.3+)

```lua
table.move(a1, f, e, t [, a2])
-- Moves elements a1[f..e] to a2[t..t+e-f]
-- Default a2 = a1

local src = {1, 2, 3, 4, 5}
local dst = {}
table.move(src, 2, 4, 1, dst)  -- dst = {2, 3, 4}
```

### 5.5 Metamethod Respect (5.3+)

In Lua 5.3+, table library functions respect metamethods:

```lua
-- 5.1-5.2: Direct access
-- 5.3+: Uses __index/__newindex if present
```

______________________________________________________________________

## 6. Bitwise Operations

### 6.1 Availability Matrix

| Feature          | 5.1 | 5.2 | 5.3        | 5.4 |
| ---------------- | --- | --- | ---------- | --- |
| `bit32` library  | ❌  | ✅  | ❌ Removed | ❌  |
| Native operators | ❌  | ❌  | ✅         | ✅  |

### 6.2 bit32 Library (5.2 Only)

```lua
bit32.band(...)       -- AND
bit32.bor(...)        -- OR
bit32.bxor(...)       -- XOR
bit32.bnot(x)         -- NOT
bit32.lshift(x, n)    -- Left shift
bit32.rshift(x, n)    -- Right shift (logical)
bit32.arshift(x, n)   -- Arithmetic right shift
bit32.lrotate(x, n)   -- Left rotate
bit32.rrotate(x, n)   -- Right rotate
bit32.btest(...)      -- Test if AND is non-zero
bit32.extract(n, f, w) -- Extract bits
bit32.replace(n, v, f, w) -- Replace bits
```

**Important**: bit32 operates on **32-bit unsigned integers** only.

### 6.3 Native Operators (5.3+)

| Operator     | Description | Metamethod |
| ------------ | ----------- | ---------- |
| `&`          | Bitwise AND | `__band`   |
| `\|`         | Bitwise OR  | `__bor`    |
| `~` (binary) | Bitwise XOR | `__bxor`   |
| `~` (unary)  | Bitwise NOT | `__bnot`   |
| `<<`         | Left shift  | `__shl`    |
| `>>`         | Right shift | `__shr`    |

**Important**: Native operators work on **64-bit integers**.

### 6.4 Shift Behavior Differences

```lua
-- bit32 (5.2): 32-bit, wraps around
bit32.lshift(1, 32)  --> 0

-- Native (5.3+): 64-bit
1 << 32  --> 4294967296

-- Negative shifts
1 << -1  --> 0 (shift >= bit width returns 0)
1 >> -1  --> 0
```

______________________________________________________________________

## 7. Coroutine Library Changes

### 7.1 Function Availability

| Function                | 5.1 | 5.2 | 5.3 | 5.4 |
| ----------------------- | --- | --- | --- | --- |
| `coroutine.create`      | ✅  | ✅  | ✅  | ✅  |
| `coroutine.resume`      | ✅  | ✅  | ✅  | ✅  |
| `coroutine.yield`       | ✅  | ✅  | ✅  | ✅  |
| `coroutine.status`      | ✅  | ✅  | ✅  | ✅  |
| `coroutine.wrap`        | ✅  | ✅  | ✅  | ✅  |
| `coroutine.running`     | ✅  | ✅  | ✅  | ✅  |
| `coroutine.isyieldable` | ❌  | ❌  | ✅  | ✅  |
| `coroutine.close`       | ❌  | ❌  | ❌  | ✅  |

### 7.2 coroutine.running() Changes

```lua
-- 5.1
coroutine.running()  --> thread or nil (nil in main thread)

-- 5.2+
coroutine.running()  --> thread, boolean (always returns thread; boolean true if main)
```

### 7.3 coroutine.isyieldable() Changes

```lua
-- 5.3
coroutine.isyieldable()  --> boolean (no argument)

-- 5.4
coroutine.isyieldable([co])  --> boolean (optional coroutine argument)
```

### 7.4 coroutine.close() (5.4 Only)

```lua
local co = coroutine.create(function()
    local <close> resource = acquire()
    coroutine.yield()
end)

coroutine.resume(co)
local ok, err = coroutine.close(co)  -- Closes to-be-closed variables
```

### 7.5 Yieldable pcall/xpcall (5.2+)

| Version | Can yield from pcall | Can yield from metamethod |
| ------- | -------------------- | ------------------------- |
| 5.1     | ❌ No                | ❌ No                     |
| 5.2+    | ✅ Yes               | ✅ Yes                    |

______________________________________________________________________

## 8. Basic Functions

### 8.1 Function Availability

| Function     | 5.1 | 5.2             | 5.3 | 5.4 |
| ------------ | --- | --------------- | --- | --- |
| `setfenv`    | ✅  | ❌ Removed      | ❌  | ❌  |
| `getfenv`    | ✅  | ❌ Removed      | ❌  | ❌  |
| `loadstring` | ✅  | ❌ Use load()   | ❌  | ❌  |
| `module`     | ✅  | ❌ Deprecated   | ❌  | ❌  |
| `unpack`     | ✅  | ❌ table.unpack | ❌  | ❌  |
| `rawlen`     | ❌  | ✅              | ✅  | ✅  |
| `warn`       | ❌  | ❌              | ❌  | ✅  |

### 8.2 warn() Function (5.4 Only)

```lua
warn("This is a warning")
warn("Multiple", " ", "parts")

-- Control messages (start with '@')
warn("@off")   -- Disable warnings
warn("@on")    -- Enable warnings
```

### 8.3 load() Function Changes

| Version | Signature                                    | Mode parameter | Env parameter |
| ------- | -------------------------------------------- | -------------- | ------------- |
| 5.1     | `load(func [, chunkname])`                   | ❌             | ❌            |
| 5.2+    | `load(chunk [, chunkname [, mode [, env]]])` | ✅             | ✅            |

**Mode parameter** (5.2+):

- `"b"` - Binary chunks only
- `"t"` - Text chunks only
- `"bt"` - Both (default)

### 8.4 loadfile() Changes

| Version | Signature                               |
| ------- | --------------------------------------- |
| 5.1     | `loadfile([filename])`                  |
| 5.2+    | `loadfile([filename [, mode [, env]]])` |

### 8.5 xpcall() Changes

```lua
-- 5.1
xpcall(f, err)  -- Only function and error handler

-- 5.2+
xpcall(f, msgh, arg1, ...)  -- Can pass arguments to f
```

### 8.6 print() Changes (5.4)

In Lua 5.4, `print()` no longer calls `tostring()` - it's hardwired to convert values directly.

### 8.7 tonumber() Precision (5.3+)

```lua
tonumber("10")      --> 10 (integer in 5.3+)
tonumber("10.5")    --> 10.5 (float)
tonumber("FF", 16)  --> 255 (integer)
```

______________________________________________________________________

## 9. Module System Changes

### 9.1 package.loaders vs package.searchers

| Version | Table Name          |
| ------- | ------------------- |
| 5.1     | `package.loaders`   |
| 5.2+    | `package.searchers` |

### 9.2 module() Function

| Version | Status        |
| ------- | ------------- |
| 5.1     | ✅ Available  |
| 5.2     | ⚠️ Deprecated |
| 5.3+    | ❌ Removed    |

### 9.3 Environment Mechanism

```lua
-- 5.1: setfenv/getfenv
setfenv(1, {})  -- Change environment

-- 5.2+: _ENV mechanism
local _ENV = {}  -- Change environment
```

### 9.4 Recommended Module Pattern

```lua
-- 5.1 style (deprecated)
module("mymod", package.seeall)
function foo() end

-- 5.2+ style (recommended)
local M = {}
function M.foo() end
return M
```

______________________________________________________________________

## 10. Metamethod Changes

### 10.1 Metamethod Availability

| Metamethod       | 5.1 | 5.2 | 5.3           | 5.4        |
| ---------------- | --- | --- | ------------- | ---------- |
| `__pairs`        | ❌  | ✅  | ✅            | ✅         |
| `__ipairs`       | ❌  | ✅  | ⚠️ Deprecated | ❌ Removed |
| `__len` (tables) | ❌  | ✅  | ✅            | ✅         |
| `__gc` (tables)  | ❌  | ✅  | ✅            | ✅         |
| `__close`        | ❌  | ❌  | ❌            | ✅         |
| `__idiv`         | ❌  | ❌  | ✅            | ✅         |
| `__band`         | ❌  | ❌  | ✅            | ✅         |
| `__bor`          | ❌  | ❌  | ✅            | ✅         |
| `__bxor`         | ❌  | ❌  | ✅            | ✅         |
| `__bnot`         | ❌  | ❌  | ✅            | ✅         |
| `__shl`          | ❌  | ❌  | ✅            | ✅         |
| `__shr`          | ❌  | ❌  | ✅            | ✅         |

### 10.2 \_\_ipairs Lifecycle

```lua
-- 5.2: __ipairs introduced for custom ipairs iteration
-- 5.3: __ipairs deprecated (ipairs respects __index instead)
-- 5.4: __ipairs removed entirely
```

### 10.3 \_\_len for Tables

```lua
-- 5.1: # operator IGNORES __len for tables
-- 5.2+: # operator USES __len for tables
```

### 10.4 \_\_gc for Tables

```lua
-- 5.1: __gc only works for userdata
-- 5.2+: __gc works for tables too
```

### 10.5 \_\_close Metamethod (5.4)

```lua
local mt = {
    __close = function(self, err)
        -- err is nil if normal exit, error object otherwise
        self:cleanup()
    end
}

local <close> resource = setmetatable({}, mt)
```

### 10.6 \_\_lt No Longer Emulates \_\_le (5.4)

```lua
-- 5.1-5.3: a <= b can fall back to not (b < a) if __le missing
-- 5.4: __le must be explicitly defined
```

______________________________________________________________________

## 11. String-to-Number Coercion

### 11.1 Coercion Changes

| Context                         | 5.1-5.3                   | 5.4                       |
| ------------------------------- | ------------------------- | ------------------------- |
| Arithmetic (`+`, `-`, `*`, `/`) | ✅ Auto-coerce            | ✅ Via string metamethods |
| Bitwise operations              | N/A (5.2: bit32 converts) | ❌ Error                  |
| Comparisons                     | ❌                        | ❌                        |
| Concatenation                   | Number → String           | Number → String           |

### 11.2 Behavioral Example

```lua
-- All versions
"10" + 1      --> 11 (works, but mechanism differs in 5.4)
"a" + 1       --> Error

-- 5.4 only
"10" & 15     --> Error! (no coercion for bitwise)
```

### 11.3 Implementation Note

In Lua 5.4, string-to-number coercion for arithmetic still works, but it's implemented via the string metatable's metamethods rather than built into the arithmetic operators themselves.

______________________________________________________________________

## 12. Implementation Priority

### High Priority (Core Behavioral Differences)

1. **Number type system** (5.3+) - Integer vs Float distinction
1. **Bitwise operations** - bit32 (5.2) vs native operators (5.3+)
1. **Environment mechanism** - setfenv/getfenv (5.1) vs \_ENV (5.2+)
1. **Basic functions** - warn (5.4), rawlen (5.2+)
1. **Coroutine.close** (5.4)

### Medium Priority (Library Differences)

6. **table.pack/unpack** - Location and availability
1. **table.move** (5.3+)
1. **string.pack/unpack** (5.3+)
1. **math.randomseed** return value and algorithm (5.4)
1. **Metamethod support** - \_\_pairs, \_\_ipairs, \_\_close, \_\_gc for tables

### Lower Priority (Edge Cases)

11. **string.format %q** extended types (5.4)
01. **string.gmatch init** parameter (5.4)
01. **print() tostring** behavior (5.4)
01. **\_\_lt emulating \_\_le** removal (5.4)
01. **UTF-8 library lax parameter** (5.4)

______________________________________________________________________

## Appendix: Quick Reference by Version

### What's New in 5.2 (vs 5.1)

- `_ENV` replaces setfenv/getfenv
- `goto` statement and labels
- `bit32` library
- `table.pack`, `table.unpack` (unpack moved)
- `rawlen` function
- `__pairs`, `__ipairs`, `__len` (tables), `__gc` (tables) metamethods
- Yieldable pcall/xpcall
- `\x` and `\z` escape sequences
- Ephemeron tables
- `xpcall` takes arguments
- `load` unified signature with mode/env
- `coroutine.running` returns boolean
- `string.rep` separator parameter
- `math.log` base parameter

### What's New in 5.3 (vs 5.2)

- Integer subtype (64-bit)
- Native bitwise operators (`&`, `|`, `~`, `<<`, `>>`)
- Floor division operator (`//`)
- `bit32` library removed
- `string.pack`, `string.unpack`, `string.packsize`
- `utf8` library
- `table.move`
- `math.type`, `math.tointeger`, `math.ult`
- `math.maxinteger`, `math.mininteger`
- `math.atan` takes optional second argument (replaces atan2)
- `__ipairs` deprecated
- `ipairs` respects metamethods
- `string.dump` strip parameter
- Floats print with `.0` suffix

### What's New in 5.4 (vs 5.3)

- To-be-closed variables (`local <close>`)
- Const variables (`local <const>`)
- `__close` metamethod
- `warn()` function
- `coroutine.close()`
- `coroutine.isyieldable` optional argument
- Generational GC (default)
- xoshiro256\*\* RNG algorithm
- `math.randomseed` returns seed, takes two args
- `math.random(0)` for all bits
- `__ipairs` removed
- `__lt` no longer emulates `__le`
- String coercion via metamethods only
- `string.gmatch` init parameter
- `string.format` %q handles more types, %p for pointers
- `print` doesn't call tostring
- `utf8` lax parameter

______________________________________________________________________

## References

- [Lua 5.1 Reference Manual](https://www.lua.org/manual/5.1/)
- [Lua 5.2 Reference Manual](https://www.lua.org/manual/5.2/)
- [Lua 5.3 Reference Manual](https://www.lua.org/manual/5.3/)
- [Lua 5.4 Reference Manual](https://www.lua.org/manual/5.4/)
