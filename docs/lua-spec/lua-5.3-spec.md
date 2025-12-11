# Lua 5.3 Reference Specification

> **Reference**: Official Lua 5.3 Manual at https://www.lua.org/manual/5.3/
> **Release Date**: January 12, 2015
> **Final Version**: Lua 5.3.6 (September 14, 2020)

______________________________________________________________________

## Table of Contents

1. [Overview and Key Changes from Lua 5.2](#1-overview-and-key-changes-from-lua-52)
1. [Basic Concepts](#2-basic-concepts)
1. [Standard Libraries](#3-standard-libraries)
1. [Metamethods Reference](#4-metamethods-reference)
1. [String Patterns](#5-string-patterns)
1. [Operator Precedence](#6-operator-precedence)

______________________________________________________________________

## 1. Overview and Key Changes from Lua 5.2

### Major Changes in Lua 5.3

#### 1.1 Integer Subtype (64-bit by default)

Lua 5.3 introduces an **integer subtype** for numbers alongside floats. The type `number` now uses two internal representations:

- **Integer**: 64-bit signed integers by default (configurable at compile time)
- **Float**: Double-precision (64-bit) floating-point numbers

**Automatic Conversion Rules:**

- Lua has explicit rules about when each representation is used
- Automatic conversion occurs between integer and float as needed
- You can force a number to be a float by writing it with `.0` suffix (e.g., `2.0`)
- Use `x = x + 0.0` to convert a variable to float

**Type Checking:**

```lua
type(3)           --> "number"
type(3.0)         --> "number"
math.type(3)      --> "integer"
math.type(3.0)    --> "float"
```

**Integer Overflow:**
Integer operations wrap around on overflow:

```lua
math.maxinteger + 1 == math.mininteger  --> true
math.mininteger - 1 == math.maxinteger  --> true
```

#### 1.2 Bitwise Operators (Native Support)

Lua 5.3 adds **native bitwise operators** directly into the language (the bit32 library is removed):

| Operator | Description          | Metamethod |
| -------- | -------------------- | ---------- |
| `&`      | Bitwise AND          | `__band`   |
| `\|`     | Bitwise OR           | `__bor`    |
| `~`      | Bitwise XOR (binary) | `__bxor`   |
| `~`      | Bitwise NOT (unary)  | `__bnot`   |
| `<<`     | Left Shift           | `__shl`    |
| `>>`     | Right Shift          | `__shr`    |

**Behavior:**

- All bitwise operations convert operands to integers
- Operate on all bits of those integers
- Result is always an integer
- Both shifts fill vacant bits with zeros
- Negative displacements shift in the opposite direction
- Displacements >= integer bit width result in zero

```lua
5 & 3      --> 1   (0101 & 0011 = 0001)
5 | 3      --> 7   (0101 | 0011 = 0111)
5 ~ 3      --> 6   (0101 ^ 0011 = 0110)
~0         --> -1  (all bits set)
8 >> 2     --> 2
2 << 3     --> 16
```

#### 1.3 Floor Division Operator

**Floor Division:** `//` (metamethod: `__idiv`)

Rounds quotient toward minus infinity. **NOT** the same as C/Fortran integer division for negative numbers.

```lua
7 // 2    --> 3
-7 // 2   --> -4  (rounds toward -infinity, not toward zero)
7 / 2     --> 3.5 (regular division)
7.5 // 2  --> 3.0 (float result)
```

#### 1.4 String Pack/Unpack for Binary Data

New functions for binary serialization:

- `string.pack(fmt, v1, v2, ...)` - Pack values into binary string
- `string.unpack(fmt, s [, pos])` - Unpack values from binary string
- `string.packsize(fmt)` - Get size of packed format

#### 1.5 UTF-8 Support Library

New `utf8` library for basic UTF-8 encoding support:

- `utf8.char(...)` - Creates UTF-8 string from codepoints
- `utf8.charpattern` - Pattern matching one UTF-8 character
- `utf8.codes(s)` - Iterator over UTF-8 characters
- `utf8.codepoint(s [, i [, j]])` - Returns codepoints
- `utf8.len(s [, i [, j]])` - Returns number of UTF-8 characters
- `utf8.offset(s, n [, i])` - Returns byte offset of n-th character

#### 1.6 Integer-Specific Math Functions

New math library functions:

- `math.type(x)` - Returns "integer", "float", or nil
- `math.tointeger(x)` - Converts to integer or returns nil
- `math.ult(m, n)` - Unsigned less-than comparison
- `math.maxinteger` - Maximum integer value (9223372036854775807)
- `math.mininteger` - Minimum integer value (-9223372036854775808)

#### 1.7 Other Notable Changes

- **Table library:** Now respects metamethods; `table.move()` added
- **ipairs:** Now respects metamethods; `__ipairs` metamethod deprecated
- **math.pow:** Deprecated (use `x^y` instead)
- **math.atan:** Now takes optional second argument (replaces math.atan2)
- **Float formatting:** Floats that look like integers now print with `.0` suffix

______________________________________________________________________

## 2. Basic Concepts

### 2.1 Values and Types

Lua 5.3 has **eight basic types**:

| Type       | Description                                                                     |
| ---------- | ------------------------------------------------------------------------------- |
| `nil`      | Represents absence of value. Only `nil` and `false` are falsy.                  |
| `boolean`  | `true` or `false`                                                               |
| `number`   | Has two subtypes: **integer** (64-bit signed) and **float** (double-precision)  |
| `string`   | Immutable byte sequences. Can contain any 8-bit value including embedded zeros. |
| `function` | First-class values. Can be Lua functions or C functions.                        |
| `userdata` | Raw memory block for C data.                                                    |
| `thread`   | Independent threads of execution for coroutines.                                |
| `table`    | Associative arrays that can be indexed with any value except `nil` and NaN.     |

### 2.2 Number Type Details

The `number` type uses two internal representations:

**Integer Operations:**

- Integer arithmetic produces integers (when possible)
- Integer overflow wraps around (two's complement)
- Division `/` always produces float
- Floor division `//` produces integer when both operands are integers

**Float Operations:**

- Any operation with a float operand produces a float
- Exponentiation `^` always produces float
- Use `x + 0.0` to force float

**Conversion:**

```lua
-- Integer to float
local f = 42 + 0.0  -- 42.0

-- Float to integer (if representable)
local i = math.tointeger(42.0)  -- 42
local i = math.tointeger(42.5)  -- nil (not exact)

-- Check type
math.type(42)    --> "integer"
math.type(42.0)  --> "float"
math.type("42")  --> nil
```

### 2.3 Environments

Every chunk compiles with an external local variable `_ENV`:

- Global name `var` translates to `_ENV.var`
- Default `_ENV` is the global environment `_G`
- Can redefine `_ENV` to change global resolution

```lua
local _ENV = {print = print}  -- Restricted environment
print("hello")  -- works
os.exit()       -- error: os is nil
```

### 2.4 Garbage Collection

Lua 5.3 uses **incremental mark-and-sweep** garbage collection:

- Objects are collected when no longer reachable
- Finalizers (`__gc`) run when objects are collected
- Weak tables allow values to be collected

**Control:**

```lua
collectgarbage("collect")     -- Full collection
collectgarbage("stop")        -- Stop collector
collectgarbage("restart")     -- Restart collector
collectgarbage("count")       -- Memory in KB
collectgarbage("step", n)     -- Incremental step
collectgarbage("setpause", n) -- Set pause (percentage)
collectgarbage("setstepmul", n) -- Set step multiplier
collectgarbage("isrunning")   -- Check if running
```

### 2.5 Coroutines

Coroutines provide cooperative multitasking:

```lua
co = coroutine.create(function(x)
    print("received:", x)
    local y = coroutine.yield(x + 1)
    print("resumed with:", y)
    return x + y
end)

coroutine.resume(co, 10)  --> true, 11
coroutine.resume(co, 20)  --> true, 30
```

**States:** `"running"`, `"suspended"`, `"normal"`, `"dead"`

______________________________________________________________________

## 3. Standard Libraries

### 3.1 Basic Functions

#### `assert(v [, message])`

Issues error if `v` is false/nil. Returns all arguments otherwise.

#### `collectgarbage([opt [, arg]])`

Controls garbage collector. Options: "collect", "stop", "restart", "count", "step", "setpause", "setstepmul", "isrunning".

#### `dofile([filename])`

Executes Lua file. Returns values from chunk.

#### `error(message [, level])`

Raises error. Level: 0=no position, 1=current function (default), 2=caller.

#### `_G`

Global environment table.

#### `getmetatable(object)`

Returns metatable or `__metatable` field value.

#### `ipairs(t)`

Iterator for array part (1, 2, 3...). Respects metamethods in 5.3.

#### `load(chunk [, chunkname [, mode [, env]]])`

Loads chunk from string or function. Mode: "b" (binary), "t" (text), "bt" (both).

#### `loadfile([filename [, mode [, env]]])`

Loads chunk from file.

#### `next(table [, index])`

Returns next key-value pair. `nil` starts traversal.

#### `pairs(t)`

Iterator for all key-value pairs. Respects `__pairs` metamethod.

#### `pcall(f [, arg1, ...])`

Protected call. Returns `true, results...` or `false, error`.

#### `print(...)`

Prints arguments to stdout with tabs between.

#### `rawequal(v1, v2)`

Equality without `__eq` metamethod.

#### `rawget(table, index)`

Get without `__index` metamethod.

#### `rawlen(v)`

Length without `__len` metamethod.

#### `rawset(table, index, value)`

Set without `__newindex` metamethod. Returns table.

#### `select(index, ...)`

Returns arguments after index, or count if index is "#".

#### `setmetatable(table, metatable)`

Sets metatable. Returns table.

#### `tonumber(e [, base])`

Converts to number. Base 2-36 for strings.

```lua
tonumber("10")      --> 10 (integer)
tonumber("10.5")    --> 10.5 (float)
tonumber("FF", 16)  --> 255 (integer)
```

#### `tostring(v)`

Converts to string. Uses `__tostring` metamethod.

#### `type(v)`

Returns type as string.

#### `_VERSION`

String: `"Lua 5.3"`

#### `xpcall(f, msgh [, arg1, ...])`

Protected call with message handler.

______________________________________________________________________

### 3.2 Coroutine Library

#### `coroutine.create(f)`

Creates coroutine with body f. Returns thread.

#### `coroutine.isyieldable()`

Returns true if running coroutine can yield.

#### `coroutine.resume(co [, val1, ...])`

Starts/resumes coroutine. Returns `true, values...` or `false, error`.

#### `coroutine.running()`

Returns running coroutine and boolean (true if main thread).

#### `coroutine.status(co)`

Returns status: "running", "suspended", "normal", "dead".

#### `coroutine.wrap(f)`

Creates coroutine, returns function that resumes it.

#### `coroutine.yield(...)`

Suspends coroutine. Arguments become resume results.

______________________________________________________________________

### 3.3 Package Library

#### `require(modname)`

Loads module. Checks `package.loaded`, then searchers.

#### `package.config`

String with compile-time configurations.

#### `package.cpath`

Search path for C modules.

#### `package.loaded`

Table of loaded modules.

#### `package.loadlib(libname, funcname)`

Loads C library dynamically.

#### `package.path`

Search path for Lua modules.

#### `package.preload`

Table of preload functions.

#### `package.searchers`

Table of searcher functions.

#### `package.searchpath(name, path [, sep [, rep]])`

Searches for file in path.

______________________________________________________________________

### 3.4 String Manipulation

All indices are 1-based. Negative indices count from end.

#### `string.byte(s [, i [, j]])`

Returns byte values of characters.

```lua
string.byte("ABC", 1, 3)  --> 65, 66, 67
```

#### `string.char(...)`

Creates string from byte values.

```lua
string.char(65, 66, 67)  --> "ABC"
```

#### `string.dump(function [, strip])`

Returns bytecode. Strip removes debug info.

#### `string.find(s, pattern [, init [, plain]])`

Finds pattern. Returns indices and captures.

#### `string.format(formatstring, ...)`

Printf-style formatting.

#### `string.gmatch(s, pattern [, init])`

Iterator returning successive matches.

#### `string.gsub(s, pattern, repl [, n])`

Global substitution. Returns string and count.

#### `string.len(s)`

Returns length.

#### `string.lower(s)`

Returns lowercase.

#### `string.match(s, pattern [, init])`

Returns captures or whole match.

#### `string.pack(fmt, v1, v2, ...)` **(NEW in 5.3)**

Packs values into binary string.

**Format options:**

| Option      | Description                   |
| ----------- | ----------------------------- |
| `<`         | Little endian                 |
| `>`         | Big endian                    |
| `=`         | Native endian                 |
| `b/B`       | Signed/unsigned byte          |
| `h/H`       | Signed/unsigned short         |
| `l/L`       | Signed/unsigned long          |
| `j/J`       | lua_Integer/lua_Unsigned      |
| `i[n]/I[n]` | Signed/unsigned int (n bytes) |
| `f`         | Float                         |
| `d`         | Double                        |
| `n`         | lua_Number                    |
| `c[n]`      | Fixed string (n bytes)        |
| `z`         | Zero-terminated string        |
| `s[n]`      | String with length prefix     |
| `x`         | Padding byte                  |

```lua
local packed = string.pack("i4 i4 z", 12345, 67890, "hello")
```

#### `string.packsize(fmt)` **(NEW in 5.3)**

Returns size of packed format (no variable options).

#### `string.rep(s, n [, sep])`

Repeats string n times with separator.

#### `string.reverse(s)`

Returns reversed string.

#### `string.sub(s, i [, j])`

Returns substring.

#### `string.unpack(fmt, s [, pos])` **(NEW in 5.3)**

Unpacks values from binary string.

```lua
local a, b, str, nextpos = string.unpack("i4 i4 z", packed)
```

#### `string.upper(s)`

Returns uppercase.

______________________________________________________________________

### 3.5 UTF-8 Support **(NEW in 5.3)**

#### `utf8.char(...)`

Creates UTF-8 string from codepoints.

```lua
utf8.char(0x48, 0x65, 0x6C, 0x6C, 0x6F)  --> "Hello"
utf8.char(0x1F600)  --> "😀"
```

#### `utf8.charpattern`

Pattern matching one UTF-8 character: `"[\0-\x7F\xC2-\xFD][\x80-\xBF]*"`

#### `utf8.codes(s)`

Iterator for UTF-8 characters.

```lua
for pos, codepoint in utf8.codes("Hello") do
    print(pos, codepoint)
end
```

#### `utf8.codepoint(s [, i [, j]])`

Returns codepoints of characters.

```lua
utf8.codepoint("ABC", 1, 3)  --> 65, 66, 67
```

#### `utf8.len(s [, i [, j]])`

Returns number of UTF-8 characters, or nil+position on error.

```lua
utf8.len("Hello")  --> 5
utf8.len("😀")     --> 1
```

#### `utf8.offset(s, n [, i])`

Returns byte position of n-th character.

```lua
utf8.offset("Hello", 3)  --> 3
utf8.offset("😀X", 2)    --> 5
```

______________________________________________________________________

### 3.6 Table Manipulation

#### `table.concat(list [, sep [, i [, j]]])`

Concatenates array elements.

#### `table.insert(list, [pos,] value)`

Inserts at position (default: end).

#### `table.move(a1, f, e, t [, a2])` **(NEW in 5.3)**

Moves elements from a1[f..e] to a2[t..].

```lua
local t = {1, 2, 3, 4, 5}
table.move(t, 1, 3, 3)  --> {1, 2, 1, 2, 3}

local t2 = {}
table.move(t, 1, 3, 1, t2)  --> t2 = {1, 2, 3}
```

#### `table.pack(...)`

Returns table with elements and `n` field.

#### `table.remove(list [, pos])`

Removes element at position (default: last).

#### `table.sort(list [, comp])`

Sorts in place.

#### `table.unpack(list [, i [, j]])`

Returns elements as multiple values.

______________________________________________________________________

### 3.7 Mathematical Functions

#### Constants

```lua
math.huge        -- Infinity
math.maxinteger  -- 9223372036854775807 (NEW in 5.3)
math.mininteger  -- -9223372036854775808 (NEW in 5.3)
math.pi          -- 3.14159265358979...
```

#### Trigonometric (radians)

```lua
math.sin(x)      math.asin(x)
math.cos(x)      math.acos(x)
math.tan(x)      math.atan(y [, x])  -- atan2 merged into atan
```

#### Exponential/Logarithmic

```lua
math.exp(x)      -- e^x
math.log(x [, base])  -- Natural log or specified base
math.sqrt(x)     -- Square root
```

#### Rounding

```lua
math.floor(x)    -- Round down (returns integer if fits)
math.ceil(x)     -- Round up (returns integer if fits)
math.modf(x)     -- Integer part, fractional part
math.fmod(x, y)  -- Remainder toward zero
```

#### Comparison

```lua
math.min(x, ...)
math.max(x, ...)
math.abs(x)
```

#### Random

```lua
math.random()       -- [0, 1)
math.random(n)      -- [1, n]
math.random(m, n)   -- [m, n]
math.randomseed(x)
```

#### Conversion

```lua
math.deg(x)      -- Radians to degrees
math.rad(x)      -- Degrees to radians
```

#### Integer Functions **(NEW in 5.3)**

```lua
math.type(x)       -- "integer", "float", or nil
math.tointeger(x)  -- Convert or nil
math.ult(m, n)     -- Unsigned less-than
```

______________________________________________________________________

### 3.8 I/O Library

#### Simple Model

```lua
io.input([file])   io.output([file])
io.read(...)       io.write(...)
io.lines([file])   io.flush()
io.close([file])
```

#### Opening Files

```lua
io.open(filename [, mode])
```

Modes: "r", "w", "a", "r+", "w+", "a+", add "b" for binary.

#### File Methods

```lua
file:read(...)     file:write(...)
file:lines(...)    file:flush()
file:seek([whence [, offset]])
file:setvbuf(mode [, size])
file:close()
```

#### Read Formats

- `"n"` - Number
- `"a"` - Entire file
- `"l"` - Line without newline (default)
- `"L"` - Line with newline
- `number` - Up to n bytes

#### Standard Files

```lua
io.stdin   io.stdout   io.stderr
```

______________________________________________________________________

### 3.9 OS Library

#### `os.clock()`

CPU time in seconds.

#### `os.date([format [, time]])`

Date/time string or table. Format "\*t" returns table.

#### `os.difftime(t2, t1)`

Difference in seconds.

#### `os.execute([command])`

Runs shell command.

#### `os.exit([code [, close]])`

Exits program.

#### `os.getenv(varname)`

Environment variable value.

#### `os.remove(filename)`

Deletes file.

#### `os.rename(oldname, newname)`

Renames file.

#### `os.setlocale(locale [, category])`

Sets locale.

#### `os.time([table])`

Current time or time from table.

#### `os.tmpname()`

Temporary filename.

______________________________________________________________________

### 3.10 Debug Library

#### `debug.debug()`

Interactive debug mode.

#### `debug.gethook([thread])`

Returns hook function, mask, count.

#### `debug.getinfo([thread,] f [, what])`

Returns function info table.

#### `debug.getlocal([thread,] f, local)`

Returns name and value of local.

#### `debug.getmetatable(value)`

Returns metatable (bypasses \_\_metatable).

#### `debug.getregistry()`

Returns registry table.

#### `debug.getupvalue(f, up)`

Returns name and value of upvalue.

#### `debug.getuservalue(u)`

Returns uservalue of userdata.

#### `debug.sethook([thread,] hook, mask [, count])`

Sets debug hook.

#### `debug.setlocal([thread,] level, local, value)`

Sets local variable.

#### `debug.setmetatable(value, table)`

Sets metatable (bypasses restrictions).

#### `debug.setupvalue(f, up, value)`

Sets upvalue.

#### `debug.setuservalue(udata, value)`

Sets uservalue.

#### `debug.traceback([thread,] [message [, level]])`

Returns traceback string.

#### `debug.upvalueid(f, n)`

Returns unique identifier for upvalue.

#### `debug.upvaluejoin(f1, n1, f2, n2)`

Makes upvalues share storage.

______________________________________________________________________

## 4. Metamethods Reference

### Arithmetic Metamethods

| Metamethod | Operator                  |
| ---------- | ------------------------- |
| `__add`    | `a + b`                   |
| `__sub`    | `a - b`                   |
| `__mul`    | `a * b`                   |
| `__div`    | `a / b`                   |
| `__mod`    | `a % b`                   |
| `__pow`    | `a ^ b`                   |
| `__unm`    | `-a`                      |
| `__idiv`   | `a // b` **(NEW in 5.3)** |
| `__concat` | `a .. b`                  |
| `__len`    | `#a`                      |

### Bitwise Metamethods **(NEW in 5.3)**

| Metamethod | Operator |
| ---------- | -------- |
| `__band`   | `a & b`  |
| `__bor`    | `a \| b` |
| `__bxor`   | `a ~ b`  |
| `__bnot`   | `~a`     |
| `__shl`    | `a << b` |
| `__shr`    | `a >> b` |

### Relational Metamethods

| Metamethod | Operator |
| ---------- | -------- |
| `__eq`     | `a == b` |
| `__lt`     | `a < b`  |
| `__le`     | `a <= b` |

### Other Metamethods

| Metamethod    | Purpose                      |
| ------------- | ---------------------------- |
| `__index`     | Table/userdata indexing      |
| `__newindex`  | Table/userdata assignment    |
| `__call`      | Function call                |
| `__tostring`  | String conversion            |
| `__metatable` | Protect metatable            |
| `__gc`        | Finalizer                    |
| `__mode`      | Weak table mode              |
| `__pairs`     | Custom pairs iteration       |
| `__name`      | Type name for error messages |

______________________________________________________________________

## 5. String Patterns

### Character Classes

| Pattern | Matches                  |
| ------- | ------------------------ |
| `.`     | Any character            |
| `%a`    | Letters                  |
| `%c`    | Control characters       |
| `%d`    | Digits                   |
| `%g`    | Printable (except space) |
| `%l`    | Lowercase                |
| `%p`    | Punctuation              |
| `%s`    | Whitespace               |
| `%u`    | Uppercase                |
| `%w`    | Alphanumeric             |
| `%x`    | Hexadecimal              |
| `%z`    | Null character           |

Uppercase = complement (e.g., `%A` = non-letter)

### Quantifiers

| Quantifier | Description     |
| ---------- | --------------- |
| `*`        | 0+ (greedy)     |
| `+`        | 1+ (greedy)     |
| `-`        | 0+ (non-greedy) |
| `?`        | 0 or 1          |

### Anchors and Special

| Pattern   | Description           |
| --------- | --------------------- |
| `^`       | Start of string       |
| `$`       | End of string         |
| `%n`      | Backreference (n=1-9) |
| `%bxy`    | Balanced pair         |
| `%f[set]` | Frontier pattern      |
| `[set]`   | Character set         |
| `[^set]`  | Complement            |

______________________________________________________________________

## 6. Operator Precedence

From lowest to highest:

| Precedence | Operators              | Associativity |
| ---------- | ---------------------- | ------------- |
| 1          | `or`                   | Left          |
| 2          | `and`                  | Left          |
| 3          | `<  >  <=  >=  ~=  ==` | Left          |
| 4          | `\|`                   | Left          |
| 5          | `~`                    | Left          |
| 6          | `&`                    | Left          |
| 7          | `<<  >>`               | Left          |
| 8          | `..`                   | Right         |
| 9          | `+  -`                 | Left          |
| 10         | `*  /  //  %`          | Left          |
| 11         | `not  #  -  ~` (unary) | Unary         |
| 12         | `^`                    | Right         |

______________________________________________________________________

## Summary: Key 5.3 Features for Implementation

### Type System

- Integer and float subtypes under `number`
- `math.type()` distinguishes between integer and float
- `math.tointeger()` for safe integer conversion

### Operators

- Native bitwise: `&`, `|`, `~`, `<<`, `>>`
- Floor division: `//`
- **bit32 library removed**

### Standard Library Additions

- **String:** `string.pack`, `string.unpack`, `string.packsize`
- **UTF-8:** Complete `utf8` library
- **Table:** `table.move`
- **Math:** `math.type`, `math.tointeger`, `math.ult`, `math.maxinteger`, `math.mininteger`
- **math.atan:** Now accepts optional second argument (replaces atan2)

### Behavioral Changes

- `ipairs` respects metamethods; `__ipairs` deprecated
- `math.pow` deprecated (use `^` operator)
- Floats that look like integers print with `.0` suffix

______________________________________________________________________

## References

- Official Lua 5.3 Reference Manual: https://www.lua.org/manual/5.3/
- Lua 5.3 Source Code: https://www.lua.org/ftp/lua-5.3.6.tar.gz
