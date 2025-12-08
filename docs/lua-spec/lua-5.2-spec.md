# Lua 5.2 Complete Reference Manual

> **Source**: Official Lua 5.2 Reference Manual - https://www.lua.org/manual/5.2/manual.html
>
> **Purpose**: Complete reference for NovaSharp Lua 5.2 implementation
>
> **Last Updated**: 2025-12-07

______________________________________________________________________

## Table of Contents

1. [Basic Concepts](#basic-concepts)
1. [Key Changes from Lua 5.1](#key-changes-from-lua-51)
1. [Standard Libraries](#standard-libraries)
   - [6.1 Basic Functions](#61-basic-functions)
   - [6.2 Coroutine Manipulation](#62-coroutine-manipulation)
   - [6.3 Modules (Package Library)](#63-modules-package-library)
   - [6.4 String Manipulation](#64-string-manipulation)
   - [6.5 Table Manipulation](#65-table-manipulation)
   - [6.6 Mathematical Functions](#66-mathematical-functions)
   - [6.7 Bitwise Operations (bit32)](#67-bitwise-operations-bit32)
   - [6.8 Input and Output Facilities](#68-input-and-output-facilities)
   - [6.9 Operating System Facilities](#69-operating-system-facilities)
   - [6.10 Debug Library](#610-debug-library)

______________________________________________________________________

## Basic Concepts

### Values and Types

Lua is dynamically typed with **eight fundamental types**:

1. **nil** - Represents absence of value; only value of type nil
1. **boolean** - Contains `false` and `true`; both nil and false are false in conditions, all other values are true
1. **number** - Double-precision floating-point numbers (IEEE 754)
1. **string** - Immutable sequences of bytes; 8-bit clean (can contain any byte value including embedded zeros)
1. **function** - First-class values; can be Lua or C functions
1. **userdata** - Arbitrary C data; full userdata (Lua-managed) and light userdata (host-managed)
1. **thread** - Independent execution threads for coroutines (not OS threads)
1. **table** - Associative arrays indexed by any Lua value except nil and NaN

All values are **first-class**: they can be stored in variables, passed as arguments, and returned from functions.

**Tables** are the primary data structuring mechanism, implementing arrays, sequences, symbol tables, sets, records, graphs, and trees. Tables are heterogeneous and can contain values of all types except nil.

**Reference Types**: Tables, functions, threads, and full userdata are reference types. Variables contain references rather than copies.

### Environments and \_ENV

**Critical change from Lua 5.1**: The \_ENV mechanism replaces setfenv/getfenv.

- Any reference to a global name `var` is syntactically translated to `_ENV.var`
- Every chunk compiles with an external local variable called `_ENV` in scope
- By default, `_ENV` is initialized with the **global environment** (accessible as `_G`)
- You can define new variables and parameters with name `_ENV` to change environment resolution
- Chunks can be loaded with custom environments using `load()` or `loadfile()`

The global environment is a distinguished table stored in the C registry. Changes to the global environment affect subsequently loaded chunks but not previously loaded chunks.

### Error Handling

Lua code can explicitly generate errors via `error()` function. To catch errors, use `pcall()` or `xpcall()` for protected mode execution.

**Error objects** (or messages) propagate error information. Lua generates errors with string objects, but programs may use any value.

**xpcall()** accepts a **message handler** function that receives the original error and returns a modified error message. The handler executes before stack unwinding, allowing traceback generation and stack inspection. Message handlers are themselves protected against errors.

### Metatables and Metamethods

Every value in Lua can have a **metatable** - an ordinary table defining behavior under special operations. Metatables control:

**Arithmetic Operations**:

- `__add` - addition (+)
- `__sub` - subtraction (-)
- `__mul` - multiplication (\*)
- `__div` - division (/)
- `__mod` - modulo (%)
- `__pow` - exponentiation (^)
- `__unm` - unary negation (-)

**Comparison Operations**:

- `__eq` - equality (==)
- `__lt` - less than (\<)
- `__le` - less or equal (\<=)

**String Operations**:

- `__concat` - concatenation (..)
- `__len` - length operator (#)

**Table/Object Operations**:

- `__index` - table indexing (table[key])
- `__newindex` - table assignment (table[key] = value)
- `__call` - function calls

**Iteration (NEW in 5.2)**:

- `__pairs` - iteration via pairs()
- `__ipairs` - iteration via ipairs()

**Garbage Collection**:

- `__gc` - finalizer for collected objects
- `__mode` - controls table weakness ('k' for keys, 'v' for values, 'kv' for both)

For equality comparisons, metamethods apply only when both operands share the same type and metamethod, and are tables or full userdata.

Access to metamethods bypasses other metamethods (uses rawget()) to prevent infinite recursion.

### Garbage Collection

Lua performs automatic memory management via an **incremental mark-and-sweep collector**.

**Control Parameters**:

- **Garbage-collector pause** - Controls wait duration; larger values reduce aggressiveness (default 200%)
- **Garbage-collector step multiplier** - Controls speed relative to allocation (default 200%)

**NEW in 5.2: Experimental Generational Collection**

- Assumes objects die young
- Traverses only recent objects
- Reduces collection time but increases memory usage
- Periodic full collections prevent accumulation of old dead objects

#### Garbage-Collection Metamethods

Objects with `__gc` metamethod enter a finalization list when garbage-collected. Finalizers execute in reverse collection order (last-marked first).

Finalized objects are **resurrected** (accessible during finalization). If finalizers store objects globally, resurrection becomes permanent.

#### Weak Tables (NEW in 5.2: Ephemeron Tables)

A **weak table** contains **weak references** ignored by the garbage collector.

- Set `__mode` metatable field: 'k' for weak keys, 'v' for weak values, 'kv' for both
- Collected keys or values cause entire pairs to be removed

**Ephemeron tables** (weak keys, strong values) - NEW in 5.2:

- Values considered reachable only if their keys are reachable
- If only the value references the key, the pair is removed

Only explicitly-constructed objects (tables, userdata) are removed from weak tables. Numbers, light C functions, and strings are never removed.

**Resurrected objects** exhibit special behavior:

- Removed from weak values before finalization
- Removed from weak keys only after finalization completes

### Coroutines

Lua supports **collaborative multithreading** via coroutines - independent execution threads.

**Key Functions**:

- `coroutine.create(function)` - Creates coroutine, returns thread handle
- `coroutine.resume(thread, ...)` - Resumes/starts execution
- `coroutine.yield(...)` - Suspends execution
- `coroutine.wrap(function)` - Creates coroutine with wrapper function
- `coroutine.status(thread)` - Returns coroutine state

**Coroutine States**:

- **running** - Currently executing
- **suspended** - Paused at yield
- **normal** - Paused, but caller is different coroutine
- **dead** - Finished execution

Resume returns boolean status plus values:

- `true, results...` on success
- `false, error_message` on error

**NEW in 5.2**: Yieldable pcall and metamethods - coroutines can yield from within pcall and metamethod calls.

______________________________________________________________________

## Key Changes from Lua 5.1

### Language Syntax Changes

#### 1. Goto Statement and Labels

```lua
::label::
-- code
goto label
```

- Labels use `::name::` syntax
- Goto transfers execution to visible labels
- Cannot enter scope of local variables

#### 2. Empty Statements

```lua
;;  -- Empty statement
```

- Allows semicolons to separate statements
- Leading semicolons in blocks permitted

#### 3. String Escape Sequences

**Hexadecimal Escapes**: `\xHH`

```lua
"\x48\x65\x6C\x6C\x6F"  -- "Hello"
```

- Exactly two hexadecimal digits
- Specifies byte value

**Whitespace Skip Escape**: `\z`

```lua
"long \z
     string"  -- "long string"
```

- Skips following span of whitespace including line breaks
- Useful for indenting long literal strings

### Environment Mechanics

#### \_ENV Replaces setfenv/getfenv

**OLD (Lua 5.1)**:

```lua
setfenv(1, {})  -- Change function environment
```

**NEW (Lua 5.2)**:

```lua
local _ENV = {}  -- All globals resolve to this table
```

- Global name `var` translates to `_ENV.var`
- Each chunk has external local variable `_ENV`
- Default: `_ENV` initialized to global environment
- Custom environments via `load()` with env parameter

### Standard Library Changes

#### Basic Functions

**ADDED**:

- `rawlen(v)` - Returns length without invoking metamethods
- `load()` - New signature with environment parameter

**DEPRECATED/REMOVED**:

- `module()` - Module system deprecated
- `setfenv()` - Replaced by \_ENV
- `getfenv()` - Replaced by \_ENV
- `loadstring()` - Use load()

#### Package Library

**RENAMED**:

- `package.loaders` → `package.searchers`

**CHANGED**:

- Module system overhauled
- `require()` behavior modified for \_ENV

#### Coroutine Library

**NEW in 5.2**:

- Yieldable pcall/xpcall
- Yieldable metamethods

#### Iteration

**NEW METAMETHODS**:

- `__pairs` - Customizes pairs() iteration
- `__ipairs` - Customizes ipairs() iteration

#### Bit32 Library (NEW in 5.2)

Complete bitwise operations library added (see section 6.7).

#### Debug Library

**ADDED**:

- `debug.getuservalue(u)` - Gets userdata Lua value
- `debug.setuservalue(u, value)` - Sets userdata Lua value
- `debug.upvalueid(f, n)` - Gets unique ID for upvalue
- `debug.upvaluejoin(f1, n1, f2, n2)` - Makes upvalues share storage

### Implementation Changes

#### Light C Functions (NEW in 5.2)

Light userdata variant for C functions:

- Host-managed memory
- Not garbage-collected by Lua
- Cannot be created or modified in Lua
- Only through C API

#### Emergency Garbage Collection

Automatic collection triggering when allocation fails, attempting to free memory before reporting errors.

#### Ephemeron Tables

Weak-key tables where values are strong but only reachable through their keys (see Garbage Collection section).

______________________________________________________________________

## Standard Libraries

## 6.1 Basic Functions

The basic library provides core Lua functions available in the global environment.

### assert (v [, message])

Issues an error when value of its argument `v` is false (nil or false); otherwise returns all arguments.

**Parameters**:

- `v` - Value to test
- `message` (optional) - Error message (default: "assertion failed!")

**Returns**: All arguments if v is not false

**Example**:

```lua
assert(type(x) == "number", "x must be a number")
```

### collectgarbage (\[opt [, arg]\])

Generic interface to the garbage collector.

**Parameters**:

- `opt` - Operation string:
  - `"collect"` - Performs full garbage-collection cycle (default)
  - `"stop"` - Stops automatic collection
  - `"restart"` - Restarts automatic collection
  - `"count"` - Returns total memory in KB
  - `"step"` - Performs a garbage-collection step
  - `"setpause"` - Sets pause (arg in percentage)
  - `"setstepmul"` - Sets step multiplier (arg in percentage)
  - `"isrunning"` - Returns boolean (collector running)
  - `"generational"` - Changes to generational mode (NEW in 5.2)
  - `"incremental"` - Changes to incremental mode (NEW in 5.2)
- `arg` - Argument for specific operations

**Returns**: Depends on operation

**Example**:

```lua
collectgarbage("collect")  -- Force collection
local kb = collectgarbage("count")  -- Get memory usage
```

### dofile ([filename])

Opens the named file and executes its contents as a Lua chunk. If no filename provided, executes from stdin.

**Parameters**:

- `filename` (optional) - File to execute

**Returns**: All values returned by the chunk

**Errors**: Propagates any errors from the chunk

**Example**:

```lua
dofile("config.lua")  -- Execute config file
```

### error (message [, level])

Terminates the last protected function called and returns `message` as the error object.

**Parameters**:

- `message` - Error object (usually string)
- `level` (optional) - Where to point error (default 1):
  - `1` - Where error was called (default)
  - `2` - Where function that called error was called
  - `0` - No position information

**Example**:

```lua
error("something went wrong")
error("invalid value", 2)  -- Point to caller
```

### \_G

Global table containing the global environment. `_G._G == _G` is always true.

**Type**: table

**Example**:

```lua
_G["x"] = 10  -- Set global variable
print(_G.x)   -- Access global variable
```

### getmetatable (object)

If object does not have a metatable, returns nil. If object's metatable has a `__metatable` field, returns that value. Otherwise returns the metatable.

**Parameters**:

- `object` - Any value

**Returns**: Metatable or nil

**Example**:

```lua
local mt = getmetatable({})
local mt = getmetatable("string")  -- String metatable
```

### ipairs (t)

Returns three values: an iterator function, the table `t`, and 0. Iterates over integer keys 1, 2, ..., until first nil value.

**Parameters**:

- `t` - Table to iterate

**Returns**: iterator, table, 0

**NEW in 5.2**: Respects `__ipairs` metamethod if present

**Example**:

```lua
for i, v in ipairs({10, 20, 30}) do
  print(i, v)  -- 1 10, 2 20, 3 30
end
```

### load (chunk \[, chunkname \[, mode [, env]\]\])

Loads a chunk. **Signature changed in Lua 5.2**.

**Parameters**:

- `chunk` - String or function:
  - If string: chunk source
  - If function: Called repeatedly to get pieces (return nil/empty ends)
- `chunkname` (optional) - Name for error messages/debug (default: chunk if string, "=(load)" if function)
- `mode` (optional) - Controls loading (default: "bt"):
  - `"b"` - Binary chunks only
  - `"t"` - Text chunks only
  - `"bt"` - Both (default)
- `env` (optional) - Environment for loaded chunk (NEW in 5.2)

**Returns**: Compiled function, or nil plus error message

**Example**:

```lua
local f = load("return 2+2")
print(f())  -- 4

local f = load("return x", "chunk", "t", {x=10})
print(f())  -- 10 (uses custom environment)
```

### loadfile (\[filename \[, mode [, env]\]\])

Loads a chunk from file. Similar to load but from file source.

**Parameters**:

- `filename` (optional) - File to load (default: stdin)
- `mode` (optional) - "b", "t", or "bt" (default: "bt")
- `env` (optional) - Environment for loaded chunk (NEW in 5.2)

**Returns**: Compiled function, or nil plus error message

**Example**:

```lua
local f = loadfile("script.lua")
if f then f() end
```

### next (table [, index])

Allows traversal of all fields of a table. Returns next index and value.

**Parameters**:

- `table` - Table to traverse
- `index` - Current key (nil to start)

**Returns**: next_key, next_value (both nil when no more elements)

**Behavior**: Order is arbitrary except for numeric keys. Modifying unvisited fields during traversal is safe; behavior of modifying existing fields is undefined.

**Example**:

```lua
local t = {a=1, b=2, c=3}
local k = next(t)  -- Get first key
while k do
  print(k, t[k])
  k = next(t, k)
end
```

### pairs (t)

Returns three values: iterator function (`next`), the table `t`, and nil. Iterates over all key-value pairs.

**Parameters**:

- `t` - Table to iterate

**Returns**: next, table, nil

**NEW in 5.2**: Respects `__pairs` metamethod if present

**Example**:

```lua
for k, v in pairs({a=1, b=2}) do
  print(k, v)  -- Arbitrary order
end
```

### pcall (f [, arg1, ···])

Calls function `f` with given arguments in protected mode. Errors are caught.

**Parameters**:

- `f` - Function to call
- `arg1, ...` - Arguments to pass

**Returns**:

- Success: `true, results...`
- Failure: `false, error_message`

**NEW in 5.2**: Yieldable - coroutines can yield from within pcall

**Example**:

```lua
local ok, result = pcall(function() return 2+2 end)
if ok then print(result) end

local ok, err = pcall(error, "failed")
print(ok, err)  -- false, failed
```

### print (···)

Prints arguments to stdout using tostring. Not intended for formatted output (use string.format).

**Parameters**:

- `...` - Values to print

**Returns**: Nothing

**Example**:

```lua
print("Hello", 123, true)  -- Hello	123	true
```

### rawequal (v1, v2)

Checks equality without invoking `__eq` metamethod.

**Parameters**:

- `v1`, `v2` - Values to compare

**Returns**: boolean

**Example**:

```lua
rawequal({}, {})  -- false (different tables)
```

### rawget (table, index)

Gets real value of `table[index]` without invoking `__index` metamethod.

**Parameters**:

- `table` - Table to access
- `index` - Key

**Returns**: table[index]

**Example**:

```lua
rawget(t, "key")  -- Bypasses __index
```

### rawlen (v)

**NEW in Lua 5.2**

Returns length of object without invoking `__len` metamethod. Object must be table or string.

**Parameters**:

- `v` - Table or string

**Returns**: integer length

**Example**:

```lua
rawlen("hello")  -- 5
rawlen({1,2,3})  -- 3
```

### rawset (table, index, value)

Sets real value of `table[index]` to `value` without invoking `__newindex` metamethod.

**Parameters**:

- `table` - Table to modify
- `index` - Key
- `value` - Value to set

**Returns**: table

**Example**:

```lua
rawset(t, "key", "value")  -- Bypasses __newindex
```

### select (index, ···)

If index is number, returns all arguments after argument number `index`. If index is "#", returns total number of extra arguments.

**Parameters**:

- `index` - Number or "#"
- `...` - Arguments

**Returns**: Varies based on index

**Example**:

```lua
select(2, 10, 20, 30)  -- returns 20, 30
select("#", 10, 20, 30)  -- returns 3
```

### setmetatable (table, metatable)

Sets metatable for given table. If metatable is nil, removes metatable. If original metatable has `__metatable` field, raises error.

**Parameters**:

- `table` - Table to modify
- `metatable` - New metatable (or nil)

**Returns**: table

**Example**:

```lua
local t = {}
setmetatable(t, {__index = function() return 42 end})
print(t.anything)  -- 42
```

### tonumber (e [, base])

Tries to convert argument to number.

**Parameters**:

- `e` - Value to convert
- `base` (optional) - Base for conversion (2-36, default 10; only for string conversion)

**Returns**: Number, or nil if conversion fails

**Example**:

```lua
tonumber("123")     -- 123
tonumber("FF", 16)  -- 255
tonumber("hello")   -- nil
```

### tostring (v)

Converts any value to string in reasonable format. For complete control, use string.format.

**Parameters**:

- `v` - Value to convert

**Returns**: string

**Behavior**: If metatable has `__tostring`, calls it with v to get result

**Example**:

```lua
tostring(123)    -- "123"
tostring(true)   -- "true"
tostring({})     -- "table: 0x..."
```

### type (v)

Returns type of value as string.

**Parameters**:

- `v` - Any value

**Returns**: string - one of "nil", "number", "string", "boolean", "table", "function", "thread", "userdata"

**Example**:

```lua
type(nil)      -- "nil"
type(123)      -- "number"
type("text")   -- "string"
type({})       -- "table"
type(print)    -- "function"
```

### \_VERSION

Global string containing version information.

**Type**: string

**Value**: "Lua 5.2"

**Example**:

```lua
print(_VERSION)  -- "Lua 5.2"
```

### xpcall (f, msgh [, arg1, ···])

Calls function `f` in protected mode with message handler.

**Parameters**:

- `f` - Function to call
- `msgh` - Message handler function (receives error)
- `arg1, ...` - Arguments to pass to f

**Returns**:

- Success: `true, results...`
- Failure: `false, result_from_msgh`

**NEW in 5.2**:

- Yieldable - coroutines can yield from within xpcall
- Takes function as message handler (Lua 5.1 took stack level)

**Example**:

```lua
local function handler(err)
  return "Error: " .. tostring(err)
end

local ok, result = xpcall(function()
  error("something broke")
end, handler)
print(ok, result)  -- false, Error: something broke
```

______________________________________________________________________

## 6.2 Coroutine Manipulation

The coroutine library provides functions for coroutine manipulation. All functions are in table `coroutine`.

### coroutine.create (f)

Creates new coroutine with function `f`. Returns thread object.

**Parameters**:

- `f` - Function body for coroutine

**Returns**: thread

**Example**:

```lua
local co = coroutine.create(function()
  print("hello")
end)
```

### coroutine.resume (co [, val1, ···])

Starts or continues execution of coroutine `co`.

**Parameters**:

- `co` - Coroutine to resume
- `val1, ...` - Values passed to coroutine:
  - First resume: passed as arguments to function
  - Subsequent resumes: returned by yield

**Returns**:

- Success: `true, values...` (values from yield or return)
- Failure: `false, error_message`

**Example**:

```lua
local co = coroutine.create(function(a, b)
  print(a, b)  -- 1, 2
  local c = coroutine.yield()
  print(c)     -- 3
end)

coroutine.resume(co, 1, 2)  -- prints "1 2"
coroutine.resume(co, 3)     -- prints "3"
```

### coroutine.running ()

Returns running coroutine plus boolean (true if running coroutine is main coroutine).

**Parameters**: None

**Returns**:

- `thread, is_main` - Running coroutine and boolean
- Or `nil` in Lua 5.1 (changed in 5.2)

**Example**:

```lua
local co, ismain = coroutine.running()
print(ismain)  -- true if in main coroutine
```

### coroutine.status (co)

Returns status of coroutine as string.

**Parameters**:

- `co` - Coroutine to check

**Returns**: string - "running", "suspended", "normal", or "dead"

- **"running"** - Currently executing
- **"suspended"** - Suspended (can be resumed)
- **"normal"** - Active but not running (called another coroutine)
- **"dead"** - Finished or stopped with error

**Example**:

```lua
local co = coroutine.create(function() end)
print(coroutine.status(co))  -- "suspended"
coroutine.resume(co)
print(coroutine.status(co))  -- "dead"
```

### coroutine.wrap (f)

Creates new coroutine with function `f`. Returns function that resumes the coroutine when called.

**Parameters**:

- `f` - Function body for coroutine

**Returns**: function (wrapper)

**Behavior**: Unlike resume, wrap propagates errors to caller instead of returning false

**Example**:

```lua
local iter = coroutine.wrap(function()
  for i = 1, 3 do
    coroutine.yield(i)
  end
end)

print(iter())  -- 1
print(iter())  -- 2
print(iter())  -- 3
```

### coroutine.yield (···)

Suspends execution of calling coroutine. Values passed become extra results of resume.

**Parameters**:

- `...` - Values to return to resume

**Returns**: Values passed to next resume

**NEW in 5.2**: Can yield from within pcall, xpcall, and metamethods

**Example**:

```lua
local co = coroutine.create(function()
  print("step 1")
  local x = coroutine.yield("yielding")
  print("step 2, got", x)
end)

local ok, msg = coroutine.resume(co)  -- prints "step 1"
print(msg)  -- "yielding"

coroutine.resume(co, 42)  -- prints "step 2, got 42"
```

______________________________________________________________________

## 6.3 Modules (Package Library)

The package library provides facilities for loading modules. All functions and values are in table `package`.

### require (modname)

Loads given module. Checks `package.loaded` first; if not found, uses searchers.

**Parameters**:

- `modname` - Module name (string)

**Returns**: Value returned by module (or `true` if no value), stored in `package.loaded[modname]`

**Behavior**:

1. Checks `package.loaded[modname]` - returns if found
1. Tries each searcher in `package.searchers` until one succeeds
1. Calls loader with module name and extra data
1. Stores result in `package.loaded[modname]` (or `true` if nil)
1. Returns stored value

**Errors**: Raises error if no searcher succeeds or loader errors

**Example**:

```lua
local json = require("json")
local mymod = require("mymodule")
```

### package.config

String describing compile-time configurations for packages.

**Type**: string (5 lines)

**Format**:

1. Directory separator (default: `\` on Windows, `/` on Unix)
1. Path separator (default: `;`)
1. Template substitution marker (default: `?`)
1. Executable path marker (default: `!`)
1. Mark to ignore following text in path (default: `-`)

**Example**:

```lua
print(package.config)
-- Windows: \
--          ;
--          ?
--          !
--          -
```

### package.cpath

Path used by require to search for C loaders.

**Type**: string

**Format**: Same as PATH environment variable - list of templates separated by semicolons

**Default**: Defined at compilation; typically reads from environment variable `LUA_CPATH_5_2` or `LUA_CPATH`

**Template Markers**:

- `?` - Replaced by module name
- `?.dll` or `?.so` - Extension depends on platform

**Example**:

```lua
package.cpath = "./?.dll;C:/lua/clibs/?.dll"
```

### package.loaded

Table storing all loaded modules. Keys are module names, values are loader results.

**Type**: table

**Behavior**:

- `require` checks this table first
- Setting `package.loaded[modname] = false` prevents require from returning cached value

**Example**:

```lua
-- Unload module
package.loaded["mymodule"] = nil

-- Check if module loaded
if package.loaded["json"] then
  print("json already loaded")
end
```

### package.loadlib (libname, funcname)

Dynamically links host program with C library `libname`.

**Parameters**:

- `libname` - Library path
- `funcname` - Function name to load

**Returns**:

- Success: C function
- Failure: `nil, error_message, error_code`

**Error codes**: "open", "init"

**Note**: Low-level function; use require for standard module loading

**Example**:

```lua
local f = package.loadlib("mylib.dll", "luaopen_mylib")
if f then f() end
```

### package.path

Path used by require to search for Lua loaders.

**Type**: string

**Format**: List of templates separated by semicolons

**Default**: Defined at compilation; typically reads from environment variable `LUA_PATH_5_2` or `LUA_PATH`

**Template Markers**:

- `?` - Replaced by module name with `.` changed to directory separator
- `?.lua` - Typical pattern

**Example**:

```lua
package.path = "./?.lua;/usr/local/lua/?.lua"
```

### package.preload

Table storing loaders for specific modules.

**Type**: table

**Behavior**: `require` checks this table after `package.loaded` and before searchers. Keys are module names, values are loader functions.

**Example**:

```lua
-- Preload a module
package.preload["mymod"] = function()
  return {version = "1.0"}
end

local m = require("mymod")  -- Calls preload function
```

### package.searchers

**RENAMED from package.loaders in Lua 5.1**

Table containing searcher functions for loading modules.

**Type**: table (array)

**Behavior**: Each searcher function receives module name and returns:

- **Success**: loader function plus extra value (passed to loader)
- **Failure**: string explaining why it failed

**Default searchers**:

1. Checks `package.preload[modname]`
1. Lua searcher - uses `package.path`
1. C searcher - uses `package.cpath`
1. All-in-one searcher - uses C path for root name

**Example**:

```lua
-- Add custom searcher
table.insert(package.searchers, 1, function(modname)
  if modname == "special" then
    return function() return {special = true} end
  else
    return string.format("\n\tno special module '%s'", modname)
  end
end)
```

______________________________________________________________________

## 6.4 String Manipulation

The string library provides functions for string manipulation. All functions are in table `string`.

Strings can also be indexed like arrays: `s[i]` gets i-th character (1-indexed). String library also sets metatable for strings where `__index` points to string table, enabling `s:upper()` syntax.

### string.byte (s \[, i [, j]\])

Returns internal numeric codes of characters `s[i]`, `s[i+1]`, ..., `s[j]`.

**Parameters**:

- `s` - String
- `i` (optional) - Start position (default: 1)
- `j` (optional) - End position (default: i)

**Returns**: integers (one per character)

**Example**:

```lua
string.byte("ABC", 1, 3)  -- 65, 66, 67
string.byte("ABC")        -- 65
```

### string.char (···)

Returns string with characters having given numeric codes.

**Parameters**:

- `...` - Numeric character codes

**Returns**: string

**Example**:

```lua
string.char(65, 66, 67)  -- "ABC"
string.char(72, 105)     -- "Hi"
```

### string.dump (function)

Returns string containing binary representation of function, suitable for later loadfile.

**Parameters**:

- `function` - Lua function

**Returns**: string (binary chunk)

**Behavior**: Function must be Lua function without upvalues

**Example**:

```lua
local f = function() return 42 end
local bin = string.dump(f)
local loaded = load(bin)
print(loaded())  -- 42
```

### string.find (s, pattern \[, init [, plain]\])

Looks for first match of `pattern` in string `s`.

**Parameters**:

- `s` - String to search
- `pattern` - Pattern to find
- `init` (optional) - Start position (default: 1, can be negative)
- `plain` (optional) - If true, pattern is plain string (default: false)

**Returns**:

- Success: start_index, end_index, captures...
- Failure: nil

**Example**:

```lua
string.find("hello world", "world")      -- 7, 11
string.find("hello world", "o")          -- 5, 5
string.find("hello world", "o", 6)       -- 8, 8
string.find("hello (world)", "(world)", 1, true)  -- 7, 13 (plain)

-- With captures
string.find("hello world", "(w.r.d)")    -- 7, 11, "world"
```

### string.format (formatstring, ···)

Returns formatted string following printf-style format string.

**Parameters**:

- `formatstring` - Format specification
- `...` - Values to format

**Returns**: string

**Format specifiers**:

- `%c` - Character (number)
- `%d`, `%i` - Decimal integer
- `%o` - Octal integer
- `%u` - Unsigned integer
- `%x` - Hexadecimal (lowercase)
- `%X` - Hexadecimal (uppercase)
- `%e` - Scientific notation (lowercase)
- `%E` - Scientific notation (uppercase)
- `%f` - Floating point
- `%g` - Shortest %e or %f (lowercase)
- `%G` - Shortest %E or %f (uppercase)
- `%q` - Quoted string suitable for Lua
- `%s` - String
- `%%` - Literal %

**Options**: Between `%` and letter: flags (`-`, `+`, ` `, `#`, `0`), width, precision

**Example**:

```lua
string.format("Hello %s", "world")        -- "Hello world"
string.format("%d", 42)                   -- "42"
string.format("%.2f", 3.14159)            -- "3.14"
string.format("%q", "a\n'b'")             -- "a\n'b'"
string.format("%05d", 42)                 -- "00042"
```

### string.gmatch (s, pattern)

Returns iterator function that each time returns next captures from `pattern` over string `s`.

**Parameters**:

- `s` - String to search
- `pattern` - Pattern with captures

**Returns**: iterator function

**Behavior**: If pattern has no captures, each call produces whole match

**Example**:

```lua
for word in string.gmatch("hello world lua", "%a+") do
  print(word)  -- "hello", "world", "lua"
end

local s = "from=world, to=Lua"
for k, v in string.gmatch(s, "(%w+)=(%w+)") do
  print(k, v)  -- "from", "world" then "to", "Lua"
end
```

### string.gsub (s, pattern, repl [, n])

Returns copy of `s` where all (or first `n`) occurrences of `pattern` are replaced by `repl`.

**Parameters**:

- `s` - String
- `pattern` - Pattern to match
- `repl` - Replacement (string, table, or function)
- `n` (optional) - Maximum replacements (default: all)

**Returns**: string, count (number of substitutions)

**Replacement types**:

- **String**: Can contain `%n` for nth capture, `%0` for whole match, `%%` for literal %
- **Table**: Pattern match used as key; value used as replacement
- **Function**: Called with captures; return value used as replacement

**Example**:

```lua
string.gsub("hello world", "world", "Lua")     -- "hello Lua", 1
string.gsub("hello hello", "hello", "hi", 1)   -- "hi hello", 1

-- With captures
string.gsub("hello world", "(%w+)", "%1 %1")   -- "hello hello world world", 2

-- With function
string.gsub("hello world", "%w+", string.upper)  -- "HELLO WORLD", 2

-- With table
string.gsub("$name is $age", "%$(%w+)", {name="John", age="30"})
-- "John is 30", 2
```

### string.len (s)

Returns string length.

**Parameters**:

- `s` - String

**Returns**: integer

**Note**: Equivalent to `#s`

**Example**:

```lua
string.len("hello")  -- 5
string.len("")       -- 0
```

### string.lower (s)

Returns copy of string with all uppercase letters changed to lowercase.

**Parameters**:

- `s` - String

**Returns**: string

**Example**:

```lua
string.lower("Hello World")  -- "hello world"
```

### string.match (s, pattern [, init])

Looks for first match of `pattern` in string. Returns captures or whole match.

**Parameters**:

- `s` - String to search
- `pattern` - Pattern
- `init` (optional) - Start position (default: 1)

**Returns**: captures, or whole match if no captures, or nil

**Example**:

```lua
string.match("hello world", "world")       -- "world"
string.match("hello world", "(%w+)")       -- "hello"
string.match("hello world", "(%w+) (%w+)")  -- "hello", "world"
```

### string.rep (s, n [, sep])

Returns string that is concatenation of `n` copies of `s` separated by `sep`.

**Parameters**:

- `s` - String to repeat
- `n` - Number of repetitions
- `sep` (optional) - Separator (default: empty string)

**Returns**: string

**Example**:

```lua
string.rep("abc", 3)       -- "abcabcabc"
string.rep("abc", 3, "-")  -- "abc-abc-abc"
```

### string.reverse (s)

Returns string with order of characters reversed.

**Parameters**:

- `s` - String

**Returns**: string

**Example**:

```lua
string.reverse("hello")  -- "olleh"
```

### string.sub (s, i [, j])

Returns substring of `s` from index `i` to `j`.

**Parameters**:

- `s` - String
- `i` - Start index (can be negative: counts from end)
- `j` (optional) - End index (default: -1 = end, can be negative)

**Returns**: string

**Example**:

```lua
string.sub("hello", 2, 4)   -- "ell"
string.sub("hello", -3)     -- "llo"
string.sub("hello", 2)      -- "ello"
string.sub("hello", -3, -2) -- "ll"
```

### string.upper (s)

Returns copy of string with all lowercase letters changed to uppercase.

**Parameters**:

- `s` - String

**Returns**: string

**Example**:

```lua
string.upper("Hello World")  -- "HELLO WORLD"
```

### Pattern Matching Syntax

Patterns are described by regular strings interpreted as patterns. Character classes, pattern items, and pattern modifiers control matching.

#### Character Classes

- `.` - Any character
- `%a` - Letters (A-Z, a-z)
- `%c` - Control characters
- `%d` - Digits (0-9)
- `%g` - Printable characters except space
- `%l` - Lowercase letters (a-z)
- `%p` - Punctuation characters
- `%s` - Space characters
- `%u` - Uppercase letters (A-Z)
- `%w` - Alphanumeric characters (A-Z, a-z, 0-9)
- `%x` - Hexadecimal digits (0-9, A-F, a-f)
- `%z` - Character with representation 0 (null)

**Uppercase**: `%A`, `%C`, `%D`, `%G`, `%L`, `%P`, `%S`, `%U`, `%W`, `%X`, `%Z` - complements (characters NOT in class)

**Magic characters**: `( ) . % + - * ? [ ] ^ $` - escape with `%`

**Character sets**: `[set]` - Union of characters in set

- `[abc]` - a, b, or c
- `[a-z]` - Any lowercase letter
- `[^set]` - Complement of set

#### Pattern Items

- Single character class - matches any character in class
- `^` - Matches beginning of string
- `$` - Matches end of string
- `%n` (n = 1-9) - Matches nth captured substring
- `%bxy` - Balanced pair x and y (e.g., `%b()` matches balanced parentheses)
- `%f[set]` - Frontier pattern (matches empty string between non-set and set character)

#### Pattern Modifiers

- `*` - 0 or more repetitions (longest match)
- `+` - 1 or more repetitions (longest match)
- `-` - 0 or more repetitions (shortest match)
- `?` - 0 or 1 occurrence (optional)

#### Captures

Patterns can be enclosed in parentheses to define captures. Matches are saved for later use.

- `(pattern)` - Captures matching substring
- `()` - Captures current position (number)

**Example patterns**:

```lua
-- Match word
"%w+"

-- Match number with optional sign and decimal
"[+-]?%d+%.?%d*"

-- Match quoted string
'".-"'  -- Non-greedy (-)

-- Match balanced parentheses
"%b()"

-- Extract date components
"(%d+)/(%d+)/(%d+)"

-- Match word boundary
"%f[%w]word%f[%W]"
```

______________________________________________________________________

## 6.5 Table Manipulation

The table library provides functions for table manipulation. All functions are in table `table`.

### table.concat (list \[, sep \[, i [, j]\]\])

Returns string that is concatenation of list elements from index `i` to `j`, separated by `sep`.

**Parameters**:

- `list` - Table (array)
- `sep` (optional) - Separator (default: empty string)
- `i` (optional) - Start index (default: 1)
- `j` (optional) - End index (default: #list)

**Returns**: string

**Behavior**: Elements must be strings or numbers

**Example**:

```lua
table.concat({"hello", "world"}, " ")     -- "hello world"
table.concat({1, 2, 3}, ", ")             -- "1, 2, 3"
table.concat({"a", "b", "c", "d"}, "-", 2, 3)  -- "b-c"
```

### table.insert (list, [pos,] value)

Inserts element `value` at position `pos` in `list`.

**Parameters**:

- `list` - Table (array)
- `pos` (optional) - Position to insert (default: #list+1, appends)
- `value` - Value to insert

**Returns**: Nothing

**Behavior**: Shifts elements up if needed

**Example**:

```lua
local t = {1, 2, 3}
table.insert(t, 2, 99)  -- {1, 99, 2, 3}
table.insert(t, 5)      -- {1, 99, 2, 3, 5}
```

### table.pack (···)

**NEW in Lua 5.2**

Returns table with all arguments. Table has field `n` with number of arguments.

**Parameters**:

- `...` - Values to pack

**Returns**: table with field `n`

**Example**:

```lua
local t = table.pack(1, 2, 3)  -- {1, 2, 3, n=3}
print(t.n)  -- 3

local t = table.pack()  -- {n=0}
local t = table.pack(1, nil, 3)  -- {1, nil, 3, n=3}
```

### table.remove (list [, pos])

Removes element at position `pos` from `list`.

**Parameters**:

- `list` - Table (array)
- `pos` (optional) - Position to remove (default: #list)

**Returns**: Removed element

**Behavior**: Shifts elements down if needed

**Example**:

```lua
local t = {1, 2, 3, 4}
local v = table.remove(t, 2)  -- v = 2, t = {1, 3, 4}
local v = table.remove(t)     -- v = 4, t = {1, 3}
```

### table.sort (list [, comp])

Sorts list elements in place from `list[1]` to `list[#list]`.

**Parameters**:

- `list` - Table (array)
- `comp` (optional) - Comparison function `comp(a, b)` returns true if `a < b` (default: \<)

**Returns**: Nothing

**Behavior**: Not stable (equal elements may change relative positions)

**Example**:

```lua
local t = {3, 1, 4, 1, 5}
table.sort(t)  -- {1, 1, 3, 4, 5}

local t = {"banana", "apple", "cherry"}
table.sort(t)  -- {"apple", "banana", "cherry"}

-- Custom comparison
local t = {{x=3}, {x=1}, {x=2}}
table.sort(t, function(a, b) return a.x < b.x end)
```

### table.unpack (list \[, i [, j]\])

**RENAMED from unpack() in Lua 5.1** (unpack is now in table library)

Returns elements from `list[i]` to `list[j]` as multiple values.

**Parameters**:

- `list` - Table (array)
- `i` (optional) - Start index (default: 1)
- `j` (optional) - End index (default: #list)

**Returns**: Multiple values

**Example**:

```lua
local a, b, c = table.unpack({10, 20, 30})  -- a=10, b=20, c=30
print(table.unpack({1, 2, 3}))              -- 1 2 3
print(table.unpack({1, 2, 3, 4, 5}, 2, 4))  -- 2 3 4
```

______________________________________________________________________

## 6.6 Mathematical Functions

The math library provides standard mathematical functions. All functions are in table `math`.

### math.abs (x)

Returns absolute value of `x`.

**Parameters**:

- `x` - Number

**Returns**: number (|x|)

**Example**:

```lua
math.abs(-5)   -- 5
math.abs(3.14) -- 3.14
```

### math.acos (x)

Returns arc cosine of `x` (in radians).

**Parameters**:

- `x` - Number in range [-1, 1]

**Returns**: number (radians)

**Example**:

```lua
math.acos(1)    -- 0
math.acos(0)    -- 1.5707963267949 (π/2)
math.acos(-1)   -- 3.1415926535898 (π)
```

### math.asin (x)

Returns arc sine of `x` (in radians).

**Parameters**:

- `x` - Number in range [-1, 1]

**Returns**: number (radians)

**Example**:

```lua
math.asin(0)    -- 0
math.asin(1)    -- 1.5707963267949 (π/2)
math.asin(-1)   -- -1.5707963267949 (-π/2)
```

### math.atan (x)

Returns arc tangent of `x` (in radians).

**Parameters**:

- `x` - Number

**Returns**: number (radians)

**Example**:

```lua
math.atan(0)    -- 0
math.atan(1)    -- 0.78539816339745 (π/4)
```

### math.atan2 (y, x)

Returns arc tangent of `y/x` (in radians), using signs to determine quadrant.

**Parameters**:

- `y` - Number
- `x` - Number

**Returns**: number (radians)

**Example**:

```lua
math.atan2(1, 1)    -- 0.78539816339745 (π/4)
math.atan2(-1, 1)   -- -0.78539816339745 (-π/4)
math.atan2(1, -1)   -- 2.3561944901923 (3π/4)
```

### math.ceil (x)

Returns smallest integer >= x.

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.ceil(3.3)   -- 4
math.ceil(-3.3)  -- -3
math.ceil(5)     -- 5
```

### math.cos (x)

Returns cosine of `x` (in radians).

**Parameters**:

- `x` - Number (radians)

**Returns**: number

**Example**:

```lua
math.cos(0)            -- 1
math.cos(math.pi)      -- -1
math.cos(math.pi / 2)  -- 0
```

### math.cosh (x)

Returns hyperbolic cosine of `x`.

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.cosh(0)  -- 1
math.cosh(1)  -- 1.5430806348152
```

### math.deg (x)

Converts angle `x` from radians to degrees.

**Parameters**:

- `x` - Number (radians)

**Returns**: number (degrees)

**Example**:

```lua
math.deg(math.pi)      -- 180
math.deg(math.pi / 2)  -- 90
```

### math.exp (x)

Returns e^x (exponential of x).

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.exp(0)  -- 1
math.exp(1)  -- 2.718281828459
math.exp(2)  -- 7.3890560989307
```

### math.floor (x)

Returns largest integer \<= x.

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.floor(3.3)   -- 3
math.floor(-3.3)  -- -4
math.floor(5)     -- 5
```

### math.fmod (x, y)

Returns remainder of division of `x` by `y` that rounds quotient towards zero.

**Parameters**:

- `x` - Number (dividend)
- `y` - Number (divisor)

**Returns**: number

**Example**:

```lua
math.fmod(10, 3)    -- 1
math.fmod(-10, 3)   -- -1
math.fmod(10, -3)   -- 1
math.fmod(10.5, 3)  -- 1.5
```

### math.frexp (x)

Returns mantissa and exponent such that x = m * 2^e, where m is in range \[0.5, 1).

**Parameters**:

- `x` - Number

**Returns**: m, e (mantissa, exponent)

**Example**:

```lua
local m, e = math.frexp(8)  -- m = 0.5, e = 4 (8 = 0.5 * 2^4)
local m, e = math.frexp(10) -- m = 0.625, e = 4 (10 = 0.625 * 2^4)
```

### math.huge

Value representing positive infinity (larger than any numeric value).

**Type**: number constant

**Example**:

```lua
print(math.huge)         -- inf
print(1 / 0 == math.huge) -- true
```

### math.ldexp (m, e)

Returns m * 2^e (inverse of frexp).

**Parameters**:

- `m` - Number (mantissa)
- `e` - Number (exponent)

**Returns**: number

**Example**:

```lua
math.ldexp(0.5, 4)   -- 8
math.ldexp(0.625, 4) -- 10
```

### math.log (x [, base])

Returns logarithm of `x` in given base.

**Parameters**:

- `x` - Number
- `base` (optional) - Logarithm base (default: e, natural logarithm)

**Returns**: number

**NEW in 5.2**: Optional base parameter

**Example**:

```lua
math.log(math.exp(1))  -- 1 (ln(e) = 1)
math.log(10)           -- 2.302585092994 (ln(10))
math.log(100, 10)      -- 2 (log10(100) = 2)
math.log(8, 2)         -- 3 (log2(8) = 3)
```

### math.max (x, ···)

Returns maximum value among arguments.

**Parameters**:

- `x, ...` - Numbers

**Returns**: number

**Example**:

```lua
math.max(1, 5, 3)   -- 5
math.max(-1, -5)    -- -1
```

### math.min (x, ···)

Returns minimum value among arguments.

**Parameters**:

- `x, ...` - Numbers

**Returns**: number

**Example**:

```lua
math.min(1, 5, 3)   -- 1
math.min(-1, -5)    -- -5
```

### math.modf (x)

Returns integral part of `x` and fractional part of `x`.

**Parameters**:

- `x` - Number

**Returns**: integer_part, fractional_part

**Example**:

```lua
local i, f = math.modf(3.7)   -- i = 3, f = 0.7
local i, f = math.modf(-3.7)  -- i = -3, f = -0.7
```

### math.pi

Value of π (pi).

**Type**: number constant

**Value**: 3.1415926535898

**Example**:

```lua
local circumference = 2 * math.pi * radius
```

### math.pow (x, y)

Returns x^y.

**Parameters**:

- `x` - Number (base)
- `y` - Number (exponent)

**Returns**: number

**Note**: Can also use `^` operator

**Example**:

```lua
math.pow(2, 3)    -- 8
math.pow(10, 2)   -- 100
math.pow(4, 0.5)  -- 2
```

### math.rad (x)

Converts angle `x` from degrees to radians.

**Parameters**:

- `x` - Number (degrees)

**Returns**: number (radians)

**Example**:

```lua
math.rad(180)  -- 3.1415926535898 (π)
math.rad(90)   -- 1.5707963267949 (π/2)
```

### math.random (\[m [, n]\])

Returns pseudo-random number.

**Parameters**:

- No arguments: Returns real number in range \[0, 1)
- `m`: Returns integer in range [1, m]
- `m, n`: Returns integer in range [m, n]

**Returns**: number

**Example**:

```lua
math.random()        -- 0.435... (between 0 and 1)
math.random(10)      -- 7 (between 1 and 10)
math.random(5, 10)   -- 8 (between 5 and 10)
```

### math.randomseed (x)

Sets seed for pseudo-random generator. Equal seeds produce equal sequences.

**Parameters**:

- `x` - Number (seed)

**Returns**: Nothing

**Example**:

```lua
math.randomseed(os.time())  -- Use current time as seed
math.randomseed(12345)      -- Fixed seed for reproducible sequences
```

### math.sin (x)

Returns sine of `x` (in radians).

**Parameters**:

- `x` - Number (radians)

**Returns**: number

**Example**:

```lua
math.sin(0)            -- 0
math.sin(math.pi / 2)  -- 1
math.sin(math.pi)      -- 0
```

### math.sinh (x)

Returns hyperbolic sine of `x`.

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.sinh(0)  -- 0
math.sinh(1)  -- 1.1752011936438
```

### math.sqrt (x)

Returns square root of `x`.

**Parameters**:

- `x` - Number (non-negative)

**Returns**: number

**Example**:

```lua
math.sqrt(4)    -- 2
math.sqrt(2)    -- 1.4142135623731
math.sqrt(100)  -- 10
```

### math.tan (x)

Returns tangent of `x` (in radians).

**Parameters**:

- `x` - Number (radians)

**Returns**: number

**Example**:

```lua
math.tan(0)            -- 0
math.tan(math.pi / 4)  -- 1
```

### math.tanh (x)

Returns hyperbolic tangent of `x`.

**Parameters**:

- `x` - Number

**Returns**: number

**Example**:

```lua
math.tanh(0)  -- 0
math.tanh(1)  -- 0.76159415595576
```

______________________________________________________________________

## 6.7 Bitwise Operations (bit32)

**NEW in Lua 5.2**

The bit32 library provides bitwise operations on unsigned 32-bit integers. All functions are in table `bit32`.

**Important**: Arguments and results are in range [0, 2^32-1]. Negative arguments are converted modulo 2^32.

### bit32.arshift (x, disp)

Returns number `x` shifted `disp` bits to the right (arithmetic shift). Vacant bits filled with copy of sign bit.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)
- `disp` - Number of bits to shift (negative = left shift)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.arshift(8, 1)   -- 4
bit32.arshift(-8, 1)  -- 2147483644 (sign bit fills)
bit32.arshift(8, -1)  -- 16 (left shift)
```

### bit32.band (···)

Returns bitwise AND of all arguments.

**Parameters**:

- `...` - Numbers (treated as 32-bit unsigned)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.band(12, 10)        -- 8 (1100 & 1010 = 1000)
bit32.band(15, 7, 3)      -- 3 (1111 & 0111 & 0011 = 0011)
bit32.band(0xFF, 0x0F)    -- 15
```

### bit32.bnot (x)

Returns bitwise NOT of `x`.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.bnot(0)   -- 4294967295 (all bits set)
bit32.bnot(1)   -- 4294967294
bit32.bnot(255) -- 4294967040
```

### bit32.bor (···)

Returns bitwise OR of all arguments.

**Parameters**:

- `...` - Numbers (treated as 32-bit unsigned)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.bor(12, 10)    -- 14 (1100 | 1010 = 1110)
bit32.bor(1, 2, 4)   -- 7 (0001 | 0010 | 0100 = 0111)
```

### bit32.btest (···)

Returns boolean whether bitwise AND of arguments is non-zero.

**Parameters**:

- `...` - Numbers (treated as 32-bit unsigned)

**Returns**: boolean

**Behavior**: Equivalent to `bit32.band(...) ~= 0`

**Example**:

```lua
bit32.btest(12, 10)   -- true (12 & 10 = 8, non-zero)
bit32.btest(12, 2)    -- false (12 & 2 = 0)
bit32.btest(1, 2, 4)  -- false (1 & 2 & 4 = 0)
```

### bit32.bxor (···)

Returns bitwise XOR of all arguments.

**Parameters**:

- `...` - Numbers (treated as 32-bit unsigned)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.bxor(12, 10)      -- 6 (1100 ^ 1010 = 0110)
bit32.bxor(1, 1)        -- 0
bit32.bxor(15, 7, 8)    -- 0
```

### bit32.extract (n, field [, width])

Returns unsigned integer formed by `width` bits extracted from `n`, starting at bit `field`.

**Parameters**:

- `n` - Number (treated as 32-bit unsigned)
- `field` - Start bit position (0 = least significant)
- `width` (optional) - Number of bits to extract (default: 1)

**Returns**: number (extracted bits, right-aligned)

**Constraints**: `field + width` must be \<= 32

**Example**:

```lua
bit32.extract(0xFF, 4, 4)   -- 15 (extract bits 4-7: 0xF)
bit32.extract(0xABCD, 8, 8) -- 171 (extract bits 8-15: 0xAB)
bit32.extract(5, 0, 1)      -- 1 (extract bit 0)
bit32.extract(5, 1, 1)      -- 0 (extract bit 1)
bit32.extract(5, 2, 1)      -- 1 (extract bit 2)
```

### bit32.lrotate (x, disp)

Returns number `x` rotated `disp` bits to the left.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)
- `disp` - Number of bits to rotate (negative = right rotation)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.lrotate(1, 1)    -- 2
bit32.lrotate(1, 31)   -- 2147483648 (bit wraps to sign position)
bit32.lrotate(7, 29)   -- 3758096384
```

### bit32.lshift (x, disp)

Returns number `x` shifted `disp` bits to the left (logical shift). Vacant bits filled with zeros.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)
- `disp` - Number of bits to shift (negative = right shift)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.lshift(1, 3)   -- 8
bit32.lshift(5, 2)   -- 20
bit32.lshift(1, -1)  -- 0 (right shift)
```

### bit32.replace (n, v, field [, width])

Returns copy of `n` with `width` bits starting at `field` replaced by `v`.

**Parameters**:

- `n` - Number (treated as 32-bit unsigned) - destination
- `v` - Number (treated as 32-bit unsigned) - source value
- `field` - Start bit position (0 = least significant)
- `width` (optional) - Number of bits to replace (default: 1)

**Returns**: number (32-bit unsigned integer)

**Constraints**: `field + width` must be \<= 32

**Example**:

```lua
bit32.replace(0xFF, 0, 4, 4)   -- 240 (0xF0: clear bits 4-7)
bit32.replace(0, 15, 4, 4)     -- 240 (0xF0: set bits 4-7)
bit32.replace(0xFFFF, 0xAB, 8, 8)  -- 43775 (0xABFF)
```

### bit32.rrotate (x, disp)

Returns number `x` rotated `disp` bits to the right.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)
- `disp` - Number of bits to rotate (negative = left rotation)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.rrotate(8, 1)   -- 4
bit32.rrotate(1, 1)   -- 2147483648 (bit wraps around)
bit32.rrotate(7, 2)   -- 3221225473
```

### bit32.rshift (x, disp)

Returns number `x` shifted `disp` bits to the right (logical shift). Vacant bits filled with zeros.

**Parameters**:

- `x` - Number (treated as 32-bit unsigned)
- `disp` - Number of bits to shift (negative = left shift)

**Returns**: number (32-bit unsigned integer)

**Example**:

```lua
bit32.rshift(8, 1)   -- 4
bit32.rshift(16, 2)  -- 4
bit32.rshift(8, -1)  -- 16 (left shift)
```

______________________________________________________________________

## 6.8 Input and Output Facilities

The I/O library provides functions for file manipulation. Two styles supported:

1. **Implicit file handles** - Operations on default input/output files
1. **Explicit file handles** - Use file objects returned by io.open

All functions are in table `io`.

### io.close ([file])

Closes file or default output file.

**Parameters**:

- `file` (optional) - File handle (default: default output file)

**Returns**:

- Success: true
- Failure: nil, error_message, error_code

**Example**:

```lua
local f = io.open("file.txt", "w")
f:write("hello")
io.close(f)
-- Or: f:close()
```

### io.flush ()

Flushes default output file.

**Parameters**: None

**Returns**: Nothing

**Example**:

```lua
io.write("text")
io.flush()  -- Ensure written to disk
```

### io.input ([file])

Opens file (by name) for reading or sets default input file.

**Parameters**:

- `file` (optional) - Filename (string) or file handle
  - If string: opens file in read mode and sets as default input
  - If handle: sets as default input
  - If omitted: returns current default input

**Returns**: Current default input file handle

**Example**:

```lua
io.input("input.txt")   -- Open and set default input
local line = io.read()  -- Read from default input

io.input():close()      -- Close current input
```

### io.lines ([filename, ···])

Opens file in read mode and returns iterator function.

**Parameters**:

- `filename` (optional) - File to read (default: default input file)
- `...` (optional) - Read formats (same as file:read)

**Returns**: iterator function

**Behavior**: Each call returns next line. At EOF, closes file and returns nil. If filename omitted, doesn't close file at EOF.

**Example**:

```lua
-- Read all lines from file
for line in io.lines("file.txt") do
  print(line)
end

-- Read numbers
for num in io.lines("numbers.txt", "*n") do
  print(num * 2)
end
```

### io.open (filename [, mode])

Opens file in specified mode.

**Parameters**:

- `filename` - File path
- `mode` (optional) - Open mode string (default: "r"):
  - `"r"` - Read mode
  - `"w"` - Write mode (overwrites)
  - `"a"` - Append mode
  - `"r+"` - Update mode (read/write, file must exist)
  - `"w+"` - Update mode (read/write, creates/overwrites)
  - `"a+"` - Append update mode (read/write)
  - Add `"b"` for binary mode (e.g., "rb", "wb")

**Returns**:

- Success: file handle
- Failure: nil, error_message, error_code

**Example**:

```lua
local f, err = io.open("file.txt", "r")
if not f then
  print("Error:", err)
else
  local content = f:read("*a")
  f:close()
end
```

### io.output ([file])

Opens file (by name) for writing or sets default output file.

**Parameters**:

- `file` (optional) - Filename (string) or file handle
  - If string: opens file in write mode and sets as default output
  - If handle: sets as default output
  - If omitted: returns current default output

**Returns**: Current default output file handle

**Example**:

```lua
io.output("output.txt")  -- Open and set default output
io.write("hello\n")      -- Write to default output
```

### io.popen (prog [, mode])

Starts program `prog` in separate process and returns file handle for reading/writing.

**Parameters**:

- `prog` - Program command
- `mode` (optional) - "r" for reading (default) or "w" for writing

**Returns**: file handle

**Behavior**: Platform-dependent; not available on all systems

**Example**:

```lua
local handle = io.popen("ls -la")
local result = handle:read("*a")
handle:close()
print(result)
```

### io.read (···)

Reads from default input file.

**Parameters**:

- `...` - Read formats:
  - `"*n"` - Reads number
  - `"*a"` - Reads whole file
  - `"*l"` - Reads next line (without newline)
  - `"*L"` - Reads next line (with newline)
  - number - Reads that many characters

**Returns**: Read values, or nil on EOF

**Example**:

```lua
io.input("file.txt")
local num = io.read("*n")       -- Read number
local line = io.read("*l")      -- Read line
local all = io.read("*a")       -- Read all
local chars = io.read(10)       -- Read 10 characters
```

### io.tmpfile ()

Returns handle for temporary file opened in update mode. File is automatically deleted when program ends.

**Parameters**: None

**Returns**: file handle

**Example**:

```lua
local tmp = io.tmpfile()
tmp:write("temporary data")
tmp:seek("set", 0)  -- Rewind
print(tmp:read("*a"))
tmp:close()
```

### io.type (obj)

Checks whether object is file handle.

**Parameters**:

- `obj` - Any value

**Returns**:

- "file" if open file handle
- "closed file" if closed file handle
- nil otherwise

**Example**:

```lua
local f = io.open("file.txt")
print(io.type(f))  -- "file"
f:close()
print(io.type(f))  -- "closed file"
print(io.type(nil))  -- nil
```

### io.write (···)

Writes values to default output file.

**Parameters**:

- `...` - Values to write (must be strings or numbers)

**Returns**: default output file handle

**Example**:

```lua
io.write("Hello", " ", "World", "\n")
io.write(string.format("Number: %d\n", 42))
```

### File Methods

File handles have the following methods:

#### file:close ()

Closes file.

**Returns**:

- Success: true
- Failure: nil, error_message, error_code

#### file:flush ()

Flushes output to file.

**Returns**:

- Success: true
- Failure: nil, error_message, error_code

#### file:lines (···)

Returns iterator function for reading file.

**Parameters**:

- `...` - Read formats (same as file:read)

**Returns**: iterator function

**Example**:

```lua
local f = io.open("file.txt")
for line in f:lines() do
  print(line)
end
f:close()
```

#### file:read (···)

Reads from file according to formats.

**Parameters**:

- `...` - Read formats (see io.read)

**Returns**: Read values, or nil on EOF

**Example**:

```lua
local f = io.open("file.txt")
local content = f:read("*a")
f:close()
```

#### file:seek (\[whence [, offset]\])

Sets and gets file position.

**Parameters**:

- `whence` (optional) - Base position (default: "cur"):
  - `"set"` - Beginning of file
  - `"cur"` - Current position
  - `"end"` - End of file
- `offset` (optional) - Offset from whence (default: 0)

**Returns**: Final file position (bytes from beginning), or nil on error

**Example**:

```lua
local f = io.open("file.txt")
f:seek("set", 0)      -- Go to beginning
local pos = f:seek()  -- Get current position
f:seek("end", -10)    -- Go to 10 bytes before end
f:close()
```

#### file:setvbuf (mode [, size])

Sets buffering mode for output file.

**Parameters**:

- `mode` - Buffering mode:
  - `"no"` - No buffering
  - `"full"` - Full buffering
  - `"line"` - Line buffering
- `size` (optional) - Buffer size in bytes

**Returns**: Success or failure

**Example**:

```lua
local f = io.open("output.txt", "w")
f:setvbuf("line")  -- Line buffering
f:write("hello\n")
f:close()
```

#### file:write (···)

Writes values to file.

**Parameters**:

- `...` - Values to write (must be strings or numbers)

**Returns**: file handle, or nil plus error message on error

**Example**:

```lua
local f = io.open("file.txt", "w")
f:write("Line 1\n", "Line 2\n")
f:close()
```

______________________________________________________________________

## 6.9 Operating System Facilities

The OS library provides operating system functions. All functions are in table `os`.

### os.clock ()

Returns CPU time used by program in seconds.

**Parameters**: None

**Returns**: number (seconds)

**Example**:

```lua
local start = os.clock()
-- ... some work ...
local elapsed = os.clock() - start
print("CPU time:", elapsed)
```

### os.date (\[format [, time]\])

Returns string or table containing date and time.

**Parameters**:

- `format` (optional) - Format string (default: "%c"):
  - If starts with `!`, uses UTC instead of local time
  - If `"*t"`, returns table
  - Otherwise, strftime format string
- `time` (optional) - Time value (default: current time)

**Returns**: string or table

**Format codes** (strftime):

- `%a` - Abbreviated weekday name
- `%A` - Full weekday name
- `%b` - Abbreviated month name
- `%B` - Full month name
- `%c` - Date and time
- `%d` - Day of month (01-31)
- `%H` - Hour (00-23)
- `%I` - Hour (01-12)
- `%M` - Minute (00-59)
- `%m` - Month (01-12)
- `%p` - AM or PM
- `%S` - Second (00-59)
- `%w` - Weekday (0-6, Sunday is 0)
- `%x` - Date
- `%X` - Time
- `%Y` - Full year
- `%y` - Two-digit year (00-99)
- `%z` - Time zone offset
- `%%` - Literal %

**Table fields** (when format is "\*t"):

- `year`, `month`, `day`, `hour`, `min`, `sec`, `wday`, `yday`, `isdst`

**Example**:

```lua
print(os.date())           -- "12/07/25 10:30:00" (locale-dependent)
print(os.date("%Y-%m-%d")) -- "2025-12-07"
print(os.date("!%Y-%m-%d %H:%M:%S"))  -- UTC time

local t = os.date("*t")
print(t.year, t.month, t.day)  -- 2025 12 7
```

### os.difftime (t2, t1)

Returns number of seconds from time `t1` to time `t2`.

**Parameters**:

- `t2` - Time value
- `t1` - Time value

**Returns**: number (seconds, may be negative)

**Example**:

```lua
local t1 = os.time()
-- ... wait ...
local t2 = os.time()
print("Elapsed:", os.difftime(t2, t1), "seconds")
```

### os.execute ([command])

Executes shell command.

**Parameters**:

- `command` (optional) - Command to execute

**Returns**:

- If command is nil: boolean (true if shell available)
- Otherwise: true/nil, exit_type, status_code
  - exit_type: "exit" (normal) or "signal" (killed by signal)
  - status_code: exit code or signal number

**Example**:

```lua
-- Check if shell available
if os.execute() then
  print("Shell available")
end

-- Execute command
local ok, exit_type, code = os.execute("ls -la")
if ok then
  print("Success")
else
  print("Failed:", exit_type, code)
end
```

### os.exit (\[code [, close]\])

Terminates host program.

**Parameters**:

- `code` (optional) - Exit code (default: true = success):
  - `true` - Success (EXIT_SUCCESS)
  - `false` - Failure (EXIT_FAILURE)
  - number - Specific exit code
- `close` (optional) - If true, closes Lua state before exit (default: false)

**Returns**: Never returns

**Example**:

```lua
os.exit()        -- Exit with success
os.exit(1)       -- Exit with code 1
os.exit(false)   -- Exit with failure
os.exit(0, true) -- Close Lua state and exit
```

### os.getenv (varname)

Returns value of environment variable.

**Parameters**:

- `varname` - Environment variable name

**Returns**: string or nil

**Example**:

```lua
local path = os.getenv("PATH")
local home = os.getenv("HOME")
print(home)
```

### os.remove (filename)

Deletes file or empty directory.

**Parameters**:

- `filename` - Path to file/directory

**Returns**:

- Success: true
- Failure: nil, error_message, error_code

**Example**:

```lua
local ok, err = os.remove("temp.txt")
if not ok then
  print("Error:", err)
end
```

### os.rename (oldname, newname)

Renames file or directory.

**Parameters**:

- `oldname` - Current name
- `newname` - New name

**Returns**:

- Success: true
- Failure: nil, error_message, error_code

**Example**:

```lua
local ok, err = os.rename("old.txt", "new.txt")
if not ok then
  print("Error:", err)
end
```

### os.setlocale (locale [, category])

Sets current locale of program.

**Parameters**:

- `locale` - Locale string (system-dependent):
  - `""` - System default locale
  - `"C"` - Minimal C locale
  - nil - Just queries current locale
- `category` (optional) - Category to set (default: "all"):
  - `"all"` - All categories
  - `"collate"` - Collation
  - `"ctype"` - Character types
  - `"monetary"` - Monetary formatting
  - `"numeric"` - Numeric formatting
  - `"time"` - Date/time formatting

**Returns**: locale string, or nil if operation fails

**Example**:

```lua
print(os.setlocale(nil))       -- Query current locale
os.setlocale("en_US.UTF-8")    -- Set locale
os.setlocale("", "numeric")    -- Set numeric to system default
```

### os.time ([table])

Returns current time or time from table.

**Parameters**:

- `table` (optional) - Date/time table with fields:
  - Required: `year`, `month`, `day`
  - Optional: `hour` (default 12), `min` (default 0), `sec` (default 0), `isdst`

**Returns**: number (time value, typically seconds since epoch)

**Example**:

```lua
local now = os.time()  -- Current time
print(now)

local t = os.time({year=2025, month=12, day=7, hour=10, min=30})
print(t)
```

### os.tmpname ()

Returns string with filename for temporary file.

**Parameters**: None

**Returns**: string (filename)

**Behavior**: File is not created; caller must create and remove it

**Warning**: May have security issues; use io.tmpfile() when possible

**Example**:

```lua
local tempname = os.tmpname()
local f = io.open(tempname, "w")
f:write("temp data")
f:close()
os.remove(tempname)
```

______________________________________________________________________

## 6.10 Debug Library

The debug library provides functions for debugging and introspection. **Warning**: These functions should be used with care as they can break language semantics.

All functions are in table `debug`.

### debug.debug ()

Enters interactive debug mode. User can inspect environment and execute commands.

**Parameters**: None

**Returns**: Nothing

**Behavior**: Reads commands from stdin. Type "cont" to continue execution.

**Example**:

```lua
debug.debug()  -- Enter interactive debugger
```

### debug.gethook ([thread])

Returns current hook settings for thread.

**Parameters**:

- `thread` (optional) - Thread to query (default: current thread)

**Returns**: hook_function, mask, count

**Example**:

```lua
local f, mask, count = debug.gethook()
print(mask)  -- e.g., "lcr"
```

### debug.getinfo ([thread,] f [, what])

Returns table with information about function.

**Parameters**:

- `thread` (optional) - Thread to query
- `f` - Function or stack level (number)
- `what` (optional) - String selecting info (default: all):
  - `n` - name and namewhat
  - `S` - source, short_src, linedefined, lastlinedefined, what
  - `l` - currentline
  - `t` - istailcall
  - `u` - nups, nparams, isvararg
  - `f` - func
  - `L` - activelines

**Returns**: table with fields based on `what`

**Fields**:

- `name` - Function name
- `namewhat` - "global", "local", "method", "field", ""
- `source` - Source code location
- `short_src` - Short version of source
- `linedefined` - Line where function defined
- `lastlinedefined` - Last line of function
- `what` - "Lua", "C", "main"
- `currentline` - Current line (-1 if not available)
- `istailcall` - Boolean (true if tail call)
- `nups` - Number of upvalues
- `nparams` - Number of parameters
- `isvararg` - Boolean (true if vararg function)
- `func` - Function itself
- `activelines` - Table with active lines

**Example**:

```lua
local info = debug.getinfo(1, "nSl")
print(info.name, info.currentline)

local function f() end
local info = debug.getinfo(f, "u")
print(info.nparams, info.nups)
```

### debug.getlocal ([thread,] f, local)

Returns name and value of local variable.

**Parameters**:

- `thread` (optional) - Thread to query
- `f` - Stack level (number) or function
- `local` - Local variable index (1-based)

**Returns**: name, value (or nil if no variable at index)

**Example**:

```lua
local function test()
  local x, y = 10, 20
  local name, value = debug.getlocal(1, 1)
  print(name, value)  -- "x", 10
end
test()
```

### debug.getmetatable (value)

Returns metatable of value or nil.

**Parameters**:

- `value` - Any value

**Returns**: metatable or nil

**Note**: Unlike getmetatable(), doesn't respect \_\_metatable

**Example**:

```lua
local t = {}
setmetatable(t, {__metatable = "hidden"})
print(getmetatable(t))        -- "hidden"
print(debug.getmetatable(t))  -- table: 0x... (actual metatable)
```

### debug.getregistry ()

Returns registry table.

**Parameters**: None

**Returns**: table (the registry)

**Example**:

```lua
local reg = debug.getregistry()
-- Inspect Lua's internal registry
```

### debug.getupvalue (f, up)

Returns name and value of upvalue.

**Parameters**:

- `f` - Function
- `up` - Upvalue index (1-based)

**Returns**: name, value (or nil if no upvalue at index)

**Example**:

```lua
local x = 10
local function f()
  return x
end
local name, value = debug.getupvalue(f, 1)
print(name, value)  -- "x", 10
```

### debug.getuservalue (u)

**NEW in Lua 5.2**

Returns Lua value associated with userdata.

**Parameters**:

- `u` - Userdata

**Returns**: value (or nil if no value or u is not userdata)

**Example**:

```lua
-- Requires C API to set uservalue
local val = debug.getuservalue(userdata_obj)
```

### debug.sethook ([thread,] hook, mask [, count])

Sets hook function for debugging.

**Parameters**:

- `thread` (optional) - Thread to set hook
- `hook` - Hook function (or nil to disable)
- `mask` - String with events to hook:
  - `c` - Call event
  - `r` - Return event
  - `l` - Line event
- `count` (optional) - Call hook every `count` instructions (for line events)

**Hook function signature**: `function(event, line)`

- `event` - "call", "return", "tail return", "line", "count"
- `line` - Current line (for line events)

**Returns**: Nothing

**Example**:

```lua
local function hook(event, line)
  print(event, line)
end

debug.sethook(hook, "l")  -- Hook every line
-- ... code ...
debug.sethook()  -- Disable hook
```

### debug.setlocal ([thread,] level, local, value)

Sets value of local variable.

**Parameters**:

- `thread` (optional) - Thread to modify
- `level` - Stack level (number)
- `local` - Local variable index (1-based)
- `value` - New value

**Returns**: name of variable, or nil if no variable at index

**Example**:

```lua
local function test()
  local x = 10
  debug.setlocal(1, 1, 20)
  print(x)  -- 20
end
test()
```

### debug.setmetatable (value, table)

Sets metatable of value.

**Parameters**:

- `value` - Value to modify
- `table` - New metatable (or nil)

**Returns**: value

**Note**: Unlike setmetatable(), works on any type and doesn't respect \_\_metatable

**Example**:

```lua
local x = 5
debug.setmetatable(x, {__tostring = function() return "five" end})
-- All numbers now have this metatable
```

### debug.setupvalue (f, up, value)

Sets value of upvalue.

**Parameters**:

- `f` - Function
- `up` - Upvalue index (1-based)
- `value` - New value

**Returns**: name of upvalue, or nil if no upvalue at index

**Example**:

```lua
local x = 10
local function f()
  return x
end
debug.setupvalue(f, 1, 20)
print(f())  -- 20
```

### debug.setuservalue (udata, value)

**NEW in Lua 5.2**

Sets Lua value associated with userdata.

**Parameters**:

- `udata` - Userdata
- `value` - Value to associate

**Returns**: udata

**Example**:

```lua
debug.setuservalue(userdata_obj, {key = "value"})
```

### debug.traceback ([thread,] \[message [, level]\])

Returns string with traceback of call stack.

**Parameters**:

- `thread` (optional) - Thread to trace
- `message` (optional) - Prepended to traceback
- `level` (optional) - Stack level to start (default: 1)

**Returns**: string

**Example**:

```lua
local function a()
  local function b()
    local function c()
      print(debug.traceback("Error occurred"))
    end
    c()
  end
  b()
end
a()
```

### debug.upvalueid (f, n)

**NEW in Lua 5.2**

Returns unique identifier for upvalue.

**Parameters**:

- `f` - Function
- `n` - Upvalue index (1-based)

**Returns**: light userdata (unique ID)

**Behavior**: Different closures sharing same upvalue have same ID

**Example**:

```lua
local x = 10
local function f() return x end
local function g() return x end
local id1 = debug.upvalueid(f, 1)
local id2 = debug.upvalueid(g, 1)
print(id1 == id2)  -- true (share same upvalue)
```

### debug.upvaluejoin (f1, n1, f2, n2)

**NEW in Lua 5.2**

Makes upvalue `n1` of function `f1` refer to upvalue `n2` of function `f2`.

**Parameters**:

- `f1` - First function
- `n1` - Upvalue index in f1 (1-based)
- `f2` - Second function
- `n2` - Upvalue index in f2 (1-based)

**Returns**: Nothing

**Behavior**: After call, f1's upvalue n1 shares storage with f2's upvalue n2

**Example**:

```lua
local x = 10
local y = 20
local function f() return x end
local function g() return y end
print(f())  -- 10
debug.upvaluejoin(f, 1, g, 1)  -- f now uses g's upvalue
print(f())  -- 20
```

______________________________________________________________________

## Summary of Lua 5.2 Key Features

### Major Language Changes

1. **\_ENV replaces setfenv/getfenv** - Global variable mechanism completely redesigned
1. **goto and labels** - Explicit control flow with ::label::
1. **Ephemeron tables** - Weak-key tables with special reachability semantics
1. **Yieldable pcall/xpcall** - Coroutines can yield from protected calls
1. **Emergency garbage collection** - Automatic collection on allocation failure

### New Standard Library Functions

- **rawlen()** - Length without metamethods
- **table.pack()** / **table.unpack()** - Pack/unpack with n field
- **math.log(x, base)** - Logarithm with optional base
- **bit32 library** - Complete 32-bit bitwise operations (12 functions)
- **\_\_pairs / \_\_ipairs metamethods** - Customize iteration
- **debug.getuservalue() / setuservalue()** - Userdata Lua values
- **debug.upvalueid() / upvaluejoin()** - Upvalue identity and sharing

### Deprecated/Removed from Lua 5.1

- **module()** - Module function removed
- **setfenv() / getfenv()** - Replaced by \_ENV
- **loadstring()** - Use load() instead
- **unpack()** - Moved to table.unpack()
- **package.loaders** - Renamed to package.searchers

### String Escape Sequences (NEW)

- **\\xHH** - Hexadecimal byte escape
- **\\z** - Skip following whitespace

______________________________________________________________________

## References

**Official Documentation**:

- Lua 5.2 Reference Manual: https://www.lua.org/manual/5.2/manual.html

**Related Documentation**:

- Lua 5.1 Reference Manual: https://www.lua.org/manual/5.1/
- Lua 5.3 Reference Manual: https://www.lua.org/manual/5.3/
- Lua 5.4 Reference Manual: https://www.lua.org/manual/5.4/

**Implementation Notes for NovaSharp**:

- Ensure \_ENV mechanics properly implemented in compiler
- Implement bit32 library for 5.2 compatibility
- Support ephemeron tables in garbage collector
- Enable yieldable pcall/xpcall in VM
- Implement \_\_pairs and \_\_ipairs metamethod support
- Add rawlen() to basic library
- Rename package.loaders to package.searchers
- Support goto/label compilation and execution
- Implement \\xHH and \\z string escapes in lexer
- Add debug.getuservalue/setuservalue/upvalueid/upvaluejoin

______________________________________________________________________

**Document Version**: 1.0
**Created**: 2025-12-07
**For**: NovaSharp Lua 5.2 Implementation
**Compiled from**: Official Lua 5.2 Reference Manual (www.lua.org)
