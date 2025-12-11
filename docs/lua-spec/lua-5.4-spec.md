# Lua 5.4 Reference Specification

> **Reference**: Official Lua 5.4 Manual at https://www.lua.org/manual/5.4/
> **Release Date**: June 29, 2020
> **Current Version**: Lua 5.4.8 (June 4, 2025)

______________________________________________________________________

## Table of Contents

1. [Overview and Key Changes from Lua 5.3](#1-overview-and-key-changes-from-lua-53)
1. [Basic Concepts](#2-basic-concepts)
1. [Standard Libraries](#3-standard-libraries)
1. [Metamethods Reference](#4-metamethods-reference)
1. [String Patterns](#5-string-patterns)
1. [Operator Precedence](#6-operator-precedence)

______________________________________________________________________

## 1. Overview and Key Changes from Lua 5.3

### Major New Features in Lua 5.4

#### 1.1 To-Be-Closed Variables

**Syntax:** `local <close> var = value`

To-be-closed variables automatically call the `__close` metamethod when they go out of scope (normal exit, break, goto, return, or error).

**Behavior:**

- The value must have a `__close` metamethod, or be false/nil (ignored)
- The metamethod receives two arguments: the value and an error object (nil if no error)
- Variables are closed in **reverse order** of declaration
- Errors in closing methods generate warnings but don't stop other closings

**Use Case:** Deterministic resource cleanup (files, connections, locks).

```lua
local <close> f = io.open("file.txt", "r")
-- f is automatically closed when scope exits

local <close> lock = acquire_lock()
-- lock is released even if an error occurs

-- Multiple to-be-closed variables close in reverse order
local <close> a = resource1()  -- closed last
local <close> b = resource2()  -- closed first
```

**Implementing \_\_close:**

```lua
local mt = {
    __close = function(self, err)
        if err then
            print("Closing due to error:", err)
        end
        self:release()
    end
}

local function create_resource()
    return setmetatable({
        release = function(self) print("Released") end
    }, mt)
end
```

#### 1.2 Const Variables

**Syntax:** `local <const> var = value`

Const variables cannot be reassigned after initialization.

```lua
local <const> PI = 3.14159
PI = 3.14  -- Error: attempt to assign to const variable

local <const> config = {debug = true}
config.debug = false  -- OK: table contents can change
config = {}           -- Error: cannot reassign const
```

#### 1.3 Generational Garbage Collector

Lua 5.4 introduces a **generational garbage collector** as the new default mode:

- **Incremental mode**: Mark-and-sweep in small steps (Lua 5.3 behavior)
- **Generational mode**: Frequent minor collections for young objects, occasional major collections

**Control:**

```lua
-- Switch to generational mode
collectgarbage("generational", minormul, majormul)

-- Switch to incremental mode
collectgarbage("incremental", pause, stepmul, stepsize)
```

#### 1.4 New `warn()` Function

Lua 5.4 adds a warning system distinct from errors:

```lua
warn("This is a warning")
warn("Multiple", " ", "strings", " concatenated")

-- Control messages (start with '@')
warn("@off")   -- Disable warnings
warn("@on")    -- Enable warnings
```

#### 1.5 New `coroutine.close()`

Explicitly closes a coroutine and its to-be-closed variables:

```lua
local co = coroutine.create(function()
    local <close> resource = acquire()
    coroutine.yield()
    -- may never reach here
end)

coroutine.resume(co)
-- Close coroutine and release resource
local ok, err = coroutine.close(co)
```

#### 1.6 Random Number Generator Changes

`math.random` now uses the **xoshiro256**\*\* algorithm:

- Better statistical properties
- Random default seed (different sequences each run)
- 128-bit state (two integers)

```lua
-- New: random seed with two components
local x, y = math.randomseed()  -- Returns seed used
math.randomseed(x, y)           -- Reproduce sequence

-- New: all bits random
local allbits = math.random(0)  -- 64-bit random integer
```

#### 1.7 String-to-Number Coercion Changes

String-to-number coercion is **removed** from core arithmetic and bitwise operations:

```lua
-- Lua 5.3:
"10" + 1  --> 11

-- Lua 5.4:
"10" + 1  --> Still works! (string library metamethods)
-- But: "1" + "2" --> integer 3 (preserves type)

-- Bitwise operations do NOT coerce:
"10" & 15  --> Error in 5.4
```

#### 1.8 Userdata with Multiple User Values

Userdata can now have **multiple user values** (not just one):

- Created via `lua_newuserdatauv(L, size, nuvalue)`
- Accessed via `lua_setiuservalue(L, idx, n)` and `lua_getiuservalue(L, idx, n)`
- Zero user values is more memory-efficient

______________________________________________________________________

## 2. Basic Concepts

### 2.1 Values and Types

Lua 5.4 has **eight basic types**:

| Type       | Description                                 |
| ---------- | ------------------------------------------- |
| `nil`      | Represents absence of value.                |
| `boolean`  | `true` or `false`                           |
| `number`   | Integer (64-bit) or float (double) subtypes |
| `string`   | Immutable byte sequences                    |
| `function` | Lua or C functions                          |
| `userdata` | C data with optional multiple user values   |
| `thread`   | Coroutines                                  |
| `table`    | Associative arrays                          |

### 2.2 Variable Attributes

Lua 5.4 introduces **variable attributes** in local declarations:

```lua
local <const> x = 10      -- Cannot be reassigned
local <close> f = open()  -- Calls __close on scope exit
local <const> <close> x = v  -- Error: only one attribute allowed
```

### 2.3 To-Be-Closed Variables Detail

**The `__close` metamethod signature:**

```lua
function __close(value, err)
    -- value: the to-be-closed variable
    -- err: error object if closing due to error, nil otherwise
end
```

**Closing order:**

1. Variables close in reverse declaration order
1. Each variable closes even if previous closing raised an error
1. Multiple errors generate warnings

**Example:**

```lua
local <close> a = setmetatable({}, {__close = function() print("A") end})
local <close> b = setmetatable({}, {__close = function() print("B") end})
-- Output when scope exits: "B" then "A"
```

### 2.4 Garbage Collection

**Two modes:**

**Incremental Mode:**

```lua
collectgarbage("incremental", pause, stepmul, stepsize)
```

- `pause`: Wait before starting new cycle (percentage, default 200)
- `stepmul`: Speed relative to allocation (percentage, default 100)
- `stepsize`: Size of each step (log2 bytes, default 13 = 8KB)

**Generational Mode (NEW default):**

```lua
collectgarbage("generational", minormul, majormul)
```

- `minormul`: Minor collection frequency multiplier
- `majormul`: Major collection threshold multiplier

**Control functions:**

```lua
collectgarbage("collect")    -- Full collection
collectgarbage("stop")       -- Stop collector
collectgarbage("restart")    -- Restart collector
collectgarbage("count")      -- Memory in KB
collectgarbage("step", n)    -- Incremental step
collectgarbage("isrunning")  -- Check if running
```

### 2.5 Metatables and Metamethods

**New in 5.4:**

- `__close` metamethod for to-be-closed variables
- `__gc` now called even if not a function (generates warning)

### 2.6 Coroutines

**New function:**

```lua
coroutine.close(co)
```

Closes a suspended or dead coroutine:

- Closes all pending to-be-closed variables
- Puts coroutine in dead state
- Returns `true` on success, or `false, error` on failure

______________________________________________________________________

## 3. Standard Libraries

### 3.1 Basic Functions

#### `assert(v [, message])`

Issues error if v is false/nil. Returns all arguments otherwise.

```lua
assert(type(x) == "number", "x must be a number")
```

#### `collectgarbage([opt [, arg]])`

Controls garbage collector.

**Options:**

| Option           | Description                                     |
| ---------------- | ----------------------------------------------- |
| `"collect"`      | Full collection (default)                       |
| `"stop"`         | Stop collector                                  |
| `"restart"`      | Restart collector                               |
| `"count"`        | Memory in KB                                    |
| `"step"`         | Incremental step                                |
| `"isrunning"`    | Check if running                                |
| `"incremental"`  | Set incremental mode (pause, stepmul, stepsize) |
| `"generational"` | Set generational mode (minormul, majormul)      |

#### `dofile([filename])`

Executes Lua file. Uses stdin if no filename.

#### `error(message [, level])`

Raises error. Level controls position info.

#### `_G`

Global environment table.

#### `getmetatable(object)`

Returns metatable or `__metatable` value.

#### `ipairs(t)`

Iterator for array part (1, 2, 3...).

#### `load(chunk [, chunkname [, mode [, env]]])`

Loads chunk. Mode: "b", "t", "bt".

#### `loadfile([filename [, mode [, env]]])`

Loads chunk from file.

#### `next(table [, index])`

Returns next key-value pair.

#### `pairs(t)`

Iterator for all pairs. Uses `__pairs` metamethod.

#### `pcall(f [, arg1, ...])`

Protected call. Returns `true, results...` or `false, error`.

#### `print(...)`

Prints to stdout. **Changed:** No longer calls tostring (hardwired).

#### `rawequal(v1, v2)`

Equality without `__eq`.

#### `rawget(table, index)`

Get without `__index`.

#### `rawlen(v)`

Length without `__len`.

#### `rawset(table, index, value)`

Set without `__newindex`.

#### `select(index, ...)`

Returns args after index, or "#" for count.

#### `setmetatable(table, metatable)`

Sets metatable. Returns table.

#### `tonumber(e [, base])`

Converts to number.

#### `tostring(v)`

Converts to string. Uses `__tostring`.

#### `type(v)`

Returns type string.

#### `_VERSION`

String: `"Lua 5.4"`

#### `warn(msg1, ...)` **(NEW in 5.4)**

Emits warning. Messages starting with '@' are control:

- `"@off"`: Disable warnings
- `"@on"`: Enable warnings

```lua
warn("Something unexpected")
warn("@off")  -- Disable
warn("This won't show")
warn("@on")   -- Re-enable
```

#### `xpcall(f, msgh [, arg1, ...])`

Protected call with message handler.

______________________________________________________________________

### 3.2 Coroutine Library

#### `coroutine.close(co)` **(NEW in 5.4)**

Closes coroutine (must be suspended or dead).

```lua
local co = coroutine.create(function()
    local <close> r = resource()
    coroutine.yield()
end)
coroutine.resume(co)
coroutine.close(co)  -- Releases resource
```

#### `coroutine.create(f)`

Creates coroutine.

#### `coroutine.isyieldable([co])`

Returns true if coroutine can yield. **Changed:** Optional argument.

#### `coroutine.resume(co [, val1, ...])`

Resumes coroutine.

#### `coroutine.running()`

Returns running coroutine and main flag.

#### `coroutine.status(co)`

Returns status string.

#### `coroutine.wrap(f)`

Creates coroutine wrapper function.

#### `coroutine.yield(...)`

Yields from coroutine.

______________________________________________________________________

### 3.3 Package Library

#### `require(modname)`

Loads module.

#### `package.config`

Configuration string.

#### `package.cpath`

C module search path.

#### `package.loaded`

Loaded modules table.

#### `package.loadlib(libname, funcname)`

Loads C library.

#### `package.path`

Lua module search path.

#### `package.preload`

Preload functions table.

#### `package.searchers`

Searcher functions table.

#### `package.searchpath(name, path [, sep [, rep]])`

Searches for file.

______________________________________________________________________

### 3.4 String Manipulation

#### `string.byte(s [, i [, j]])`

Returns byte values.

#### `string.char(...)`

Creates string from bytes.

#### `string.dump(function [, strip])`

Returns bytecode.

#### `string.find(s, pattern [, init [, plain]])`

Finds pattern.

#### `string.format(formatstring, ...)`

Printf-style formatting.

**New/changed specifiers:**

- `%p`: Formats pointer (tables, userdata, etc.)
- `%q`: Now handles booleans, nil, numbers

```lua
string.format("%p", {})     --> "0x..."
string.format("%q", true)   --> "true"
string.format("%q", nil)    --> "nil"
```

#### `string.gmatch(s, pattern [, init])`

Iterator for matches. **Changed:** Optional init position.

#### `string.gsub(s, pattern, repl [, n])`

Substitution.

#### `string.len(s)`

Returns length.

#### `string.lower(s)`

Returns lowercase.

#### `string.match(s, pattern [, init])`

Returns captures.

#### `string.pack(fmt, v1, v2, ...)`

Packs binary data.

#### `string.packsize(fmt)`

Returns pack size.

#### `string.rep(s, n [, sep])`

Repeats string.

#### `string.reverse(s)`

Reverses string.

#### `string.sub(s, i [, j])`

Returns substring.

#### `string.unpack(fmt, s [, pos])`

Unpacks binary data.

#### `string.upper(s)`

Returns uppercase.

______________________________________________________________________

### 3.5 UTF-8 Support

**Changed in 5.4:** Functions now accept optional `lax` parameter to allow surrogates.

#### `utf8.char(...)`

Creates UTF-8 string.

#### `utf8.charpattern`

Pattern for one UTF-8 character.

#### `utf8.codes(s [, lax])`

Iterator for codepoints. **Changed:** lax parameter.

#### `utf8.codepoint(s [, i [, j [, lax]]])`

Returns codepoints. **Changed:** lax parameter.

#### `utf8.len(s [, i [, j [, lax]]])`

Returns character count. **Changed:** lax parameter.

#### `utf8.offset(s, n [, i])`

Returns byte offset.

______________________________________________________________________

### 3.6 Table Manipulation

#### `table.concat(list [, sep [, i [, j]]])`

Concatenates elements.

#### `table.insert(list, [pos,] value)`

Inserts element.

#### `table.move(a1, f, e, t [, a2])`

Moves elements.

#### `table.pack(...)`

Packs arguments with n field.

#### `table.remove(list [, pos])`

Removes element.

#### `table.sort(list [, comp])`

Sorts in place.

#### `table.unpack(list [, i [, j]])`

Unpacks elements.

______________________________________________________________________

### 3.7 Mathematical Functions

#### Constants

```lua
math.huge        -- Infinity
math.maxinteger  -- Maximum integer
math.mininteger  -- Minimum integer
math.pi          -- Pi
```

#### Functions

| Function                     | Description            |
| ---------------------------- | ---------------------- |
| `math.abs(x)`                | Absolute value         |
| `math.acos(x)`               | Arc cosine             |
| `math.asin(x)`               | Arc sine               |
| `math.atan(y [, x])`         | Arc tangent            |
| `math.ceil(x)`               | Round up               |
| `math.cos(x)`                | Cosine                 |
| `math.deg(x)`                | Radians to degrees     |
| `math.exp(x)`                | e^x                    |
| `math.floor(x)`              | Round down             |
| `math.fmod(x, y)`            | Remainder              |
| `math.log(x [, base])`       | Logarithm              |
| `math.max(x, ...)`           | Maximum                |
| `math.min(x, ...)`           | Minimum                |
| `math.modf(x)`               | Integer and fraction   |
| `math.rad(x)`                | Degrees to radians     |
| `math.random([m [, n]])`     | Random number          |
| `math.randomseed([x [, y]])` | Set seed **(CHANGED)** |
| `math.sin(x)`                | Sine                   |
| `math.sqrt(x)`               | Square root            |
| `math.tan(x)`                | Tangent                |
| `math.tointeger(x)`          | Convert to integer     |
| `math.type(x)`               | Number subtype         |
| `math.ult(m, n)`             | Unsigned less-than     |

#### `math.random([m [, n]])` Changes

**New behavior:**

```lua
math.random()      -- [0, 1)
math.random(n)     -- [1, n]
math.random(m, n)  -- [m, n]
math.random(0)     -- All bits random (NEW)
```

#### `math.randomseed([x [, y]])` Changes

**New signature and behavior:**

```lua
-- No arguments: generates random seed
local x, y = math.randomseed()

-- With arguments: 128-bit seed
math.randomseed(12345, 67890)

-- Returns seed components for reproducibility
local x, y = math.randomseed()
-- Later, to reproduce:
math.randomseed(x, y)
```

______________________________________________________________________

### 3.8 I/O Library

#### `io.close([file])`

Closes file.

#### `io.flush()`

Flushes output.

#### `io.input([file])`

Sets/gets input file.

#### `io.lines([filename, ...])`

Returns line iterator. **Changed:** Returns 4 values.

#### `io.open(filename [, mode])`

Opens file.

#### `io.output([file])`

Sets/gets output file.

#### `io.popen(prog [, mode])`

Opens process.

#### `io.read(...)`

Reads from input.

#### `io.tmpfile()`

Creates temp file.

#### `io.type(obj)`

Checks file type.

#### `io.write(...)`

Writes to output.

#### File Methods

```lua
file:close()
file:flush()
file:lines(...)
file:read(...)
file:seek([whence [, offset]])
file:setvbuf(mode [, size])
file:write(...)
```

______________________________________________________________________

### 3.9 OS Library

#### `os.clock()`

CPU time.

#### `os.date([format [, time]])`

Date string or table.

#### `os.difftime(t2, t1)`

Time difference.

#### `os.execute([command])`

Runs command.

#### `os.exit([code [, close]])`

Exits program.

#### `os.getenv(varname)`

Environment variable.

#### `os.remove(filename)`

Deletes file.

#### `os.rename(oldname, newname)`

Renames file.

#### `os.setlocale(locale [, category])`

Sets locale.

#### `os.time([table])`

Current time.

#### `os.tmpname()`

Temp filename.

______________________________________________________________________

### 3.10 Debug Library

#### `debug.debug()`

Interactive mode.

#### `debug.gethook([thread])`

Returns hook settings.

#### `debug.getinfo([thread,] f [, what])`

Function info.

#### `debug.getlocal([thread,] f, local)`

Local variable.

#### `debug.getmetatable(value)`

Returns metatable.

#### `debug.getregistry()`

Returns registry.

#### `debug.getupvalue(f, up)`

Upvalue.

#### `debug.getuservalue(u, n)` **(CHANGED)**

Returns n-th user value. **Changed:** now takes index.

#### `debug.sethook([thread,] hook, mask [, count])`

Sets hook.

#### `debug.setlocal([thread,] level, local, value)`

Sets local.

#### `debug.setmetatable(value, table)`

Sets metatable.

#### `debug.setupvalue(f, up, value)`

Sets upvalue.

#### `debug.setuservalue(udata, value, n)` **(CHANGED)**

Sets n-th user value. **Changed:** now takes index.

#### `debug.traceback([thread,] [message [, level]])`

Stack trace.

#### `debug.upvalueid(f, n)`

Upvalue ID.

#### `debug.upvaluejoin(f1, n1, f2, n2)`

Join upvalues.

______________________________________________________________________

## 4. Metamethods Reference

### All Metamethods

| Metamethod    | Purpose                |
| ------------- | ---------------------- |
| `__add`       | Addition `+`           |
| `__sub`       | Subtraction `-`        |
| `__mul`       | Multiplication `*`     |
| `__div`       | Division `/`           |
| `__mod`       | Modulo `%`             |
| `__pow`       | Power `^`              |
| `__unm`       | Unary minus `-`        |
| `__idiv`      | Floor division `//`    |
| `__band`      | Bitwise AND `&`        |
| `__bor`       | Bitwise OR `\|`        |
| `__bxor`      | Bitwise XOR `~`        |
| `__bnot`      | Bitwise NOT `~`        |
| `__shl`       | Shift left `<<`        |
| `__shr`       | Shift right `>>`       |
| `__concat`    | Concatenation `..`     |
| `__len`       | Length `#`             |
| `__eq`        | Equality `==`          |
| `__lt`        | Less than `<`          |
| `__le`        | Less or equal `<=`     |
| `__index`     | Table indexing         |
| `__newindex`  | Table assignment       |
| `__call`      | Function call          |
| `__tostring`  | String conversion      |
| `__metatable` | Protect metatable      |
| `__gc`        | Finalizer              |
| `__close`     | To-be-closed **(NEW)** |
| `__mode`      | Weak table mode        |
| `__pairs`     | Custom pairs           |
| `__name`      | Type name              |

### `__close` Metamethod **(NEW in 5.4)**

```lua
function __close(value, err)
    -- value: the variable being closed
    -- err: error object (nil if normal exit)
    if err then
        print("Closing due to error:", err)
    end
    value:cleanup()
end
```

### `__lt` No Longer Emulates `__le`

In Lua 5.4, you must define `__le` explicitly if needed. Lua no longer falls back to `not (b < a)`.

______________________________________________________________________

## 5. String Patterns

Same as Lua 5.3. See [String Patterns](#5-string-patterns) in the 5.3 spec.

### Character Classes

| Pattern | Matches                |
| ------- | ---------------------- |
| `.`     | Any character          |
| `%a`    | Letters                |
| `%c`    | Control                |
| `%d`    | Digits                 |
| `%g`    | Printable except space |
| `%l`    | Lowercase              |
| `%p`    | Punctuation            |
| `%s`    | Whitespace             |
| `%u`    | Uppercase              |
| `%w`    | Alphanumeric           |
| `%x`    | Hexadecimal            |

### Quantifiers

| Quantifier | Description   |
| ---------- | ------------- |
| `*`        | 0+ greedy     |
| `+`        | 1+ greedy     |
| `-`        | 0+ non-greedy |
| `?`        | 0 or 1        |

### Anchors

| Pattern   | Description |
| --------- | ----------- |
| `^`       | Start       |
| `$`       | End         |
| `%b()`    | Balanced    |
| `%f[set]` | Frontier    |

______________________________________________________________________

## 6. Operator Precedence

From lowest to highest:

| Precedence | Operators              |
| ---------- | ---------------------- |
| 1          | `or`                   |
| 2          | `and`                  |
| 3          | `<  >  <=  >=  ~=  ==` |
| 4          | `\|`                   |
| 5          | `~`                    |
| 6          | `&`                    |
| 7          | `<<  >>`               |
| 8          | `..`                   |
| 9          | `+  -`                 |
| 10         | `*  /  //  %`          |
| 11         | `not  #  -  ~` (unary) |
| 12         | `^`                    |

______________________________________________________________________

## Incompatibilities from Lua 5.3 to 5.4

### Language Incompatibilities

1. **String-to-number coercion** removed from arithmetic/bitwise (string library provides via metamethods)
1. **Decimal integer overflow** reads as float (use hex for wrap)
1. **`__lt` no longer emulates `__le`**
1. **Numerical for loop** control variable never wraps
1. **Goto labels** cannot shadow visible labels
1. **`__gc`** called even if not function (generates warning)

### Library Incompatibilities

1. **`print`** doesn't call tostring (hardwired)
1. **`math.random`** uses xoshiro256\*\* with random seed
1. **`utf8` library** rejects surrogates by default (use lax)
1. **`collectgarbage`** setpause/setstepmul deprecated
1. **`io.lines`** returns 4 values

### API Incompatibilities

1. **Userdata** can have multiple user values
1. **`lua_resume`** has extra out parameter
1. **`lua_version`** returns number directly
1. **`LUA_ERRGCMM`** removed
1. **`LUA_GCINC`** replaces old GC options

______________________________________________________________________

## Summary: Key 5.4 Features for Implementation

### New Language Features

- **To-be-closed variables:** `local <close> x = v`
- **Const variables:** `local <const> x = v`
- **`__close` metamethod**

### New Standard Library

- **`warn()`** function
- **`coroutine.close()`**
- **`math.randomseed()`** returns seed
- **`math.random(0)`** all bits random

### Behavioral Changes

- **Generational GC** as default
- **xoshiro256**\*\* random algorithm
- **`__lt` doesn't emulate `__le`**
- **String coercion via metamethods only**

### C API Changes

- Multiple user values for userdata
- `lua_resume` extra parameter
- Removed `LUA_ERRGCMM`

______________________________________________________________________

## References

- Official Lua 5.4 Reference Manual: https://www.lua.org/manual/5.4/
- Lua 5.4 Source Code: https://www.lua.org/ftp/lua-5.4.8.tar.gz
