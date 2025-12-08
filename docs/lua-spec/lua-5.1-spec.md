# Lua 5.1 Reference Specification

> **Reference**: Official Lua 5.1 Manual at https://www.lua.org/manual/5.1/
> **Release Date**: February 21, 2006
> **Final Version**: Lua 5.1.5 (February 17, 2012)

______________________________________________________________________

## Table of Contents

1. [Basic Concepts](#1-basic-concepts)
1. [The Language](#2-the-language)
1. [Standard Libraries](#3-standard-libraries)
1. [Metamethods Reference](#4-metamethods-reference)
1. [String Patterns](#5-string-patterns)
1. [Operator Precedence](#6-operator-precedence)

______________________________________________________________________

## 1. Basic Concepts

### 1.1 Values and Types

Lua is a dynamically typed language. There are **eight basic types**:

| Type       | Description                                                                                                 |
| ---------- | ----------------------------------------------------------------------------------------------------------- |
| `nil`      | Represents absence of value. Only `nil` and `false` are falsy.                                              |
| `boolean`  | `true` or `false`                                                                                           |
| `number`   | Double-precision floating-point (IEEE 754). Lua 5.1 has no integer subtype.                                 |
| `string`   | Immutable byte sequences. Can contain any 8-bit value including embedded zeros.                             |
| `function` | First-class values. Can be Lua functions or C functions.                                                    |
| `userdata` | Raw memory block for C data. Two kinds: full userdata (managed by Lua) and light userdata (just a pointer). |
| `thread`   | Independent threads of execution for coroutines.                                                            |
| `table`    | Associative arrays that can be indexed with any value except `nil` and NaN.                                 |

**Type Checking**:

```lua
type(nil)           --> "nil"
type(true)          --> "boolean"
type(3.14)          --> "number"
type("hello")       --> "string"
type(print)         --> "function"
type({})            --> "table"
type(coroutine.create(function() end))  --> "thread"
```

### 1.2 Coercion

**Automatic String-to-Number Coercion**:

- Arithmetic operations automatically convert strings to numbers
- `"10" + 1` → `11`
- `"10" * "2"` → `20`
- Fails if string is not a valid number representation

**Automatic Number-to-String Coercion**:

- Concatenation converts numbers to strings
- `"value: " .. 42` → `"value: 42"`

### 1.3 Variables

Three kinds of variables:

1. **Global variables** - Stored in `_G` environment table
1. **Local variables** - Lexically scoped
1. **Table fields** - Accessed via indexing

```lua
x = 10              -- global
local y = 20        -- local
t = {}
t.field = 30        -- table field (syntactic sugar for t["field"])
t[1] = 40           -- table field with numeric key
```

### 1.4 Environments

Every function has an associated **environment table**:

- Global variables are looked up in the environment
- Default environment is `_G`
- Can be changed with `setfenv()` / retrieved with `getfenv()`

```lua
-- Change environment of a function
local env = { print = function(x) io.write("[", x, "]\n") end }
setmetatable(env, {__index = _G})  -- fallback to _G
setfenv(1, env)  -- set environment of current function
print("hello")   -- uses env.print
```

### 1.5 Garbage Collection

Lua uses **automatic memory management** with an incremental mark-and-sweep collector.

**Garbage Collector Controls**:

```lua
collectgarbage("collect")    -- Full GC cycle
collectgarbage("stop")       -- Stop collector
collectgarbage("restart")    -- Restart collector
collectgarbage("count")      -- Returns memory in use (KB)
collectgarbage("step", n)    -- Perform step (n = step size multiplier)
collectgarbage("setpause", n)    -- Set pause (default 200 = 2x memory)
collectgarbage("setstepmul", n)  -- Set step multiplier (default 200)
```

**Weak Tables**:
Tables can have weak keys, weak values, or both. Set via `__mode` metamethod:

```lua
t = setmetatable({}, {__mode = "k"})   -- weak keys
t = setmetatable({}, {__mode = "v"})   -- weak values
t = setmetatable({}, {__mode = "kv"})  -- weak keys and values
```

### 1.6 Coroutines

Coroutines provide collaborative multitasking:

```lua
co = coroutine.create(function(x)
    print("received:", x)
    local y = coroutine.yield(x + 1)
    print("resumed with:", y)
    return x + y
end)

print(coroutine.status(co))      --> "suspended"
print(coroutine.resume(co, 10))  --> true, 11 (prints "received: 10")
print(coroutine.resume(co, 20))  --> true, 30 (prints "resumed with: 20")
print(coroutine.status(co))      --> "dead"
```

______________________________________________________________________

## 2. The Language

### 2.1 Lexical Conventions

**Identifiers**: Start with letter or underscore, followed by letters, digits, or underscores. Case-sensitive.

**Reserved Keywords** (21 total):

```
and       break     do        else      elseif    end
false     for       function  if        in        local
nil       not       or        repeat    return    then
true      until     while
```

**String Literals**:

```lua
'single quotes'
"double quotes"
[[long string
multiple lines]]
[==[long string with = signs]==]
```

**Escape Sequences**:

| Sequence | Meaning                                          |
| -------- | ------------------------------------------------ |
| `\a`     | Bell                                             |
| `\b`     | Backspace                                        |
| `\f`     | Form feed                                        |
| `\n`     | Newline                                          |
| `\r`     | Carriage return                                  |
| `\t`     | Horizontal tab                                   |
| `\v`     | Vertical tab                                     |
| `\\`     | Backslash                                        |
| `\"`     | Double quote                                     |
| `\'`     | Single quote                                     |
| `\ddd`   | Character with decimal code ddd (up to 3 digits) |

**Number Literals**:

```lua
3     3.0     3.1416     314.16e-2     0.31416E1     0xff     0x56
```

**Comments**:

```lua
-- single line comment
--[[
    multi-line
    comment
]]
--[=[
    multi-line with = for nesting
]=]
```

### 2.2 Operators

**Arithmetic Operators**:

| Operator | Description    | Example         |
| -------- | -------------- | --------------- |
| `+`      | Addition       | `5 + 3` → `8`   |
| `-`      | Subtraction    | `5 - 3` → `2`   |
| `*`      | Multiplication | `5 * 3` → `15`  |
| `/`      | Division       | `5 / 2` → `2.5` |
| `%`      | Modulo         | `5 % 3` → `2`   |
| `^`      | Exponentiation | `2 ^ 3` → `8`   |
| `-`      | Unary minus    | `-5` → `-5`     |

**Relational Operators** (return boolean):

| Operator | Description           |
| -------- | --------------------- |
| `==`     | Equal                 |
| `~=`     | Not equal             |
| `<`      | Less than             |
| `>`      | Greater than          |
| `<=`     | Less than or equal    |
| `>=`     | Greater than or equal |

**Equality Rules**:

- Different types are always different (except number coercion for some ops)
- `nil` equals only itself
- Tables, userdata, functions compared by reference
- Strings compared by content

**Logical Operators**:

| Operator | Description                                                    |
| -------- | -------------------------------------------------------------- |
| `and`    | Logical AND (short-circuit, returns first falsy or last value) |
| `or`     | Logical OR (short-circuit, returns first truthy or last value) |
| `not`    | Logical NOT (returns boolean)                                  |

```lua
-- Short-circuit evaluation
nil and "yes"      --> nil
false and "yes"    --> false
true and "yes"     --> "yes"
1 and 2            --> 2

nil or "default"   --> "default"
false or "default" --> "default"
true or "default"  --> true
1 or 2             --> 1

not nil            --> true
not false          --> true
not 0              --> false (0 is truthy!)
not ""             --> false (empty string is truthy!)
```

**String Concatenation**:

```lua
"Hello" .. " " .. "World"  --> "Hello World"
"Value: " .. 42            --> "Value: 42"
```

**Length Operator** `#`:

```lua
#"hello"        --> 5
#{1, 2, 3}      --> 3 (for sequences only)
#{1, nil, 3}    --> undefined behavior (not a sequence)
```

### 2.3 Statements

**Assignment**:

```lua
x = 1
x, y, z = 1, 2, 3
a, b = b, a         -- swap
x, y = 1, 2, 3      -- 3 is discarded
x, y, z = 1, 2      -- z is nil
```

**Control Structures**:

```lua
-- if statement
if condition then
    -- code
elseif other_condition then
    -- code
else
    -- code
end

-- while loop
while condition do
    -- code
end

-- repeat-until loop (condition checked at end, body always runs once)
repeat
    -- code
until condition

-- numeric for loop
for i = start, stop, step do  -- step defaults to 1
    -- i is local to loop
end

for i = 1, 10 do print(i) end
for i = 10, 1, -1 do print(i) end

-- generic for loop
for k, v in pairs(t) do
    print(k, v)
end

for i, v in ipairs(t) do
    print(i, v)
end

for line in io.lines("file.txt") do
    print(line)
end
```

**Loop Control**:

```lua
break    -- exits innermost loop (must be last statement in block)
-- Note: Lua 5.1 has NO continue statement
-- Note: Lua 5.1 has NO goto statement
```

**Return Statement**:

```lua
return              -- return nothing
return x            -- return single value
return x, y, z      -- return multiple values
-- must be last statement in block
```

### 2.4 Functions

**Function Definition**:

```lua
-- Global function
function name(a, b, c)
    return a + b + c
end

-- Local function
local function name(a, b, c)
    return a + b + c
end

-- Anonymous function
local f = function(a, b, c)
    return a + b + c
end

-- Method syntax (implicit self parameter)
function obj:method(a, b)
    return self.value + a + b
end
-- equivalent to:
function obj.method(self, a, b)
    return self.value + a + b
end
```

**Varargs**:

```lua
function vararg(a, b, ...)
    local args = {...}
    print("fixed:", a, b)
    print("varargs:", ...)
    print("count:", select("#", ...))
    print("from 2:", select(2, ...))
end

vararg(1, 2, 3, 4, 5)
-- fixed: 1 2
-- varargs: 3 4 5
-- count: 3
-- from 2: 4 5
```

**Multiple Return Values**:

```lua
function multi()
    return 1, 2, 3
end

a, b, c = multi()           -- a=1, b=2, c=3
a = multi()                 -- a=1 (others discarded)
t = {multi()}               -- t={1, 2, 3}
print(multi())              -- prints 1 2 3
print(multi(), "x")         -- prints 1 x (adjusted to 1)
print("x", multi())         -- prints x 1 2 3
```

**Tail Calls**:

```lua
function tail_call(n)
    if n <= 0 then return 0 end
    return tail_call(n - 1)  -- proper tail call, no stack growth
end
```

### 2.5 Tables

**Table Constructors**:

```lua
{}                          -- empty table
{1, 2, 3}                   -- array: {[1]=1, [2]=2, [3]=3}
{x = 1, y = 2}              -- record: {["x"]=1, ["y"]=2}
{[1] = "a", [2] = "b"}      -- explicit keys
{["key with spaces"] = 1}   -- string key with spaces
{[f()] = value}             -- computed key
{1, 2, x = 3, [4] = 4}      -- mixed

-- Nested tables
{
    name = "John",
    address = {
        city = "NYC",
        zip = "10001"
    }
}
```

**Table Access**:

```lua
t.field         -- syntactic sugar for t["field"]
t["field"]      -- string key
t[1]            -- numeric key
t[expr]         -- any expression as key

-- Method call
obj:method(args)    -- syntactic sugar for obj.method(obj, args)
```

______________________________________________________________________

## 3. Standard Libraries

### 3.1 Basic Functions

#### `assert(v [, message])`

Issues an error if `v` is false or nil. Returns all arguments if assertion passes.

```lua
assert(type(x) == "number", "x must be a number")
local a, b, c = assert(f())  -- propagates return values
```

#### `collectgarbage([opt [, arg]])`

Controls garbage collector. Options:

- `"collect"` - Full GC cycle
- `"stop"` - Stop collector
- `"restart"` - Restart collector
- `"count"` - Returns memory in KB (with fractional part)
- `"step"` - Perform GC step
- `"setpause"` - Set pause between cycles (percentage)
- `"setstepmul"` - Set step multiplier

#### `dofile([filename])`

Loads and executes a Lua file. Uses stdin if no filename. Returns values from chunk.

```lua
result = dofile("script.lua")
```

#### `error(message [, level])`

Raises an error with message. Level specifies error position:

- `0` - No position info
- `1` - Current function (default)
- `2` - Caller of current function
- etc.

#### `_G`

Global environment table. All global variables are fields of `_G`.

```lua
_G.newglobal = 42
print(_G["print"])  -- the print function
```

#### `getfenv([f])`

Returns environment of function `f`. If `f` is a number, returns environment of function at that stack level (1 = current).

```lua
local env = getfenv(1)  -- environment of current function
local env = getfenv(print)  -- environment of print function
```

#### `getmetatable(object)`

Returns metatable of object, or nil if none. If metatable has `__metatable` field, returns that instead.

#### `ipairs(t)`

Iterator for array part of table. Returns index, value pairs for indices 1, 2, 3... until nil value.

```lua
for i, v in ipairs({10, 20, 30}) do
    print(i, v)  -- 1 10, 2 20, 3 30
end
```

#### `load(func [, chunkname])`

Loads a chunk from a reader function. Function is called repeatedly, should return strings (concatenated) or nil to end.

```lua
local chunk = load(function() return io.read("*l") end, "=stdin")
```

#### `loadfile([filename])`

Loads chunk from file. Returns compiled chunk as function, or nil + error message.

```lua
local f, err = loadfile("script.lua")
if f then f() else print(err) end
```

#### `loadstring(string [, chunkname])`

Loads chunk from string. Returns compiled chunk as function.

```lua
local f = loadstring("return 1 + 2")
print(f())  -- 3

local f = loadstring("x = 10")
f()  -- sets global x to 10
```

#### `module(name [, ...])`

Creates a module. Sets environment to new table, registers in `package.loaded`.

```lua
-- mymodule.lua
module("mymodule", package.seeall)
function hello() print("Hello") end
```

#### `next(table [, index])`

Iterator function. Returns next key-value pair after `index`. Returns nil when done.

```lua
next({a=1, b=2}, nil)   --> "a", 1 (or "b", 2 - order undefined)
next({a=1, b=2}, "a")   --> "b", 2 (or nil if "a" was last)
```

#### `pairs(t)`

Iterator for all key-value pairs in table. Order is undefined.

```lua
for k, v in pairs({a=1, b=2, c=3}) do
    print(k, v)
end
```

#### `pcall(f, ...)`

Protected call. Calls function in protected mode. Returns `true, results...` on success or `false, error_message` on error.

```lua
local ok, result = pcall(function() return 1/0 end)
-- ok=true, result=inf

local ok, err = pcall(function() error("oops") end)
-- ok=false, err="oops"
```

#### `print(...)`

Prints arguments to stdout, separated by tabs, with newline at end. Uses `tostring` for conversion.

#### `rawequal(v1, v2)`

Checks equality without invoking `__eq` metamethod.

#### `rawget(table, index)`

Gets table value without invoking `__index` metamethod.

#### `rawset(table, index, value)`

Sets table value without invoking `__newindex` metamethod. Returns table.

#### `require(modname)`

Loads a module. Searches `package.path` for Lua modules, `package.cpath` for C modules. Caches in `package.loaded`.

```lua
local json = require("json")
local socket = require("socket")
```

#### `select(index, ...)`

If index is number, returns all arguments after that index. If `"#"`, returns count of arguments.

```lua
select(2, "a", "b", "c")   --> "b", "c"
select("#", "a", "b", "c") --> 3
```

#### `setfenv(f, table)`

Sets environment of function `f`. If `f` is number, sets environment of function at that stack level.

```lua
local env = {print = print}
setfenv(1, env)  -- change current function's environment
```

#### `setmetatable(table, metatable)`

Sets metatable of table. Pass nil to remove metatable. Returns the table.

#### `tonumber(e [, base])`

Converts to number. Base can be 2-36 for string conversion.

```lua
tonumber("42")       --> 42
tonumber("3.14")     --> 3.14
tonumber("ff", 16)   --> 255
tonumber("1010", 2)  --> 10
tonumber("hello")    --> nil
```

#### `tostring(e)`

Converts to string. Uses `__tostring` metamethod if available.

#### `type(v)`

Returns type as string: "nil", "boolean", "number", "string", "function", "table", "thread", "userdata"

#### `unpack(list [, i [, j]])`

Returns elements from table as multiple values. Default i=1, j=#list.

```lua
unpack({1, 2, 3})        --> 1, 2, 3
unpack({1, 2, 3}, 2)     --> 2, 3
unpack({1, 2, 3}, 1, 2)  --> 1, 2
```

#### `_VERSION`

String containing Lua version: `"Lua 5.1"`

#### `xpcall(f, err)`

Like pcall but with custom error handler. Error handler receives error message, can add stack trace.

```lua
local ok, result = xpcall(
    function() error("oops") end,
    function(err) return debug.traceback(err) end
)
```

______________________________________________________________________

### 3.2 Coroutine Library

#### `coroutine.create(f)`

Creates new coroutine with function f as body. Returns thread.

#### `coroutine.resume(co [, val1, ...])`

Starts or resumes coroutine. First call passes arguments to body function. Subsequent calls pass arguments as yield results.

Returns `true, values...` on success (yield values or return values), or `false, error_message` on error.

#### `coroutine.running()`

Returns currently running coroutine, or nil if in main thread.

#### `coroutine.status(co)`

Returns status string:

- `"running"` - currently executing
- `"suspended"` - suspended in yield or not started
- `"normal"` - active but not running (resumed another coroutine)
- `"dead"` - finished or stopped with error

#### `coroutine.wrap(f)`

Creates coroutine and returns function that resumes it. Unlike resume, propagates errors.

#### `coroutine.yield(...)`

Suspends coroutine. Arguments become return values of resume. Returns arguments passed to next resume.

______________________________________________________________________

### 3.3 Package Library

#### `module(name [, ...])`

Creates module. Deprecated pattern - prefer returning a table.

#### `require(modname)`

Loads module. Search order:

1. `package.loaded[modname]`
1. `package.preload[modname]`
1. Search `package.path` for Lua modules
1. Search `package.cpath` for C modules

#### `package.cpath`

Search path for C modules. Uses `?` as placeholder.

```lua
-- Default on Unix:
"./?.so;/usr/local/lib/lua/5.1/?.so"
```

#### `package.loaded`

Table of loaded modules. `require` checks and stores here.

#### `package.loaders`

Table of searcher functions. Default: preload, Lua loader, C loader, all-in-one loader.

#### `package.loadlib(libname, funcname)`

Loads C library. Returns initialization function.

#### `package.path`

Search path for Lua modules. Uses `?` as placeholder.

```lua
-- Default:
"./?.lua;/usr/local/share/lua/5.1/?.lua"
```

#### `package.preload`

Table of preload functions for modules.

#### `package.seeall(module)`

Sets `_G` as fallback for module table (via `__index`).

______________________________________________________________________

### 3.4 String Library

All string indices are 1-based. Negative indices count from end (-1 = last character).

#### `string.byte(s [, i [, j]])`

Returns numeric byte values. Default i=1, j=i.

```lua
string.byte("ABC")        --> 65
string.byte("ABC", 2)     --> 66
string.byte("ABC", 1, 3)  --> 65, 66, 67
```

#### `string.char(...)`

Returns string from byte values.

```lua
string.char(65, 66, 67)  --> "ABC"
```

#### `string.dump(function)`

Returns binary string of compiled function (bytecode).

#### `string.find(s, pattern [, init [, plain]])`

Finds pattern in string. Returns start, end indices. If pattern has captures, also returns captures.

```lua
string.find("hello world", "world")     --> 7, 11
string.find("hello world", "o", 6)      --> 8, 8
string.find("hello", ".", 1, true)      --> nil (plain search for literal ".")
string.find("hello.world", ".", 1, true) --> 6, 6
string.find("key=value", "(%w+)=(%w+)") --> 1, 9, "key", "value"
```

#### `string.format(formatstring, ...)`

Returns formatted string. Similar to C printf.

```lua
string.format("%d", 42)           --> "42"
string.format("%05d", 42)         --> "00042"
string.format("%.2f", 3.14159)    --> "3.14"
string.format("%s = %q", "x", "a\nb")  --> 'x = "a\nb"'
string.format("%x", 255)          --> "ff"
string.format("%X", 255)          --> "FF"
string.format("%o", 8)            --> "10"
string.format("%%")               --> "%"
```

Format specifiers: `%c` (char), `%d`/`%i` (integer), `%e`/`%E` (scientific), `%f` (float), `%g`/`%G` (compact), `%o` (octal), `%s` (string), `%q` (quoted), `%x`/`%X` (hex), `%%` (literal %)

#### `string.gmatch(s, pattern)`

Iterator returning successive matches.

```lua
for word in string.gmatch("hello world", "%w+") do
    print(word)  -- "hello", "world"
end

for k, v in string.gmatch("a=1 b=2", "(%w+)=(%w+)") do
    print(k, v)  -- "a" "1", "b" "2"
end
```

#### `string.gsub(s, pattern, repl [, n])`

Global substitution. Returns modified string and count. `repl` can be string, table, or function.

```lua
string.gsub("hello world", "world", "Lua")  --> "hello Lua", 1
string.gsub("hello", "l", "L", 1)           --> "heLlo", 1

-- With captures
string.gsub("hello", "(.)(.)", "%2%1")      --> "ehllo", 2

-- With table
t = {name = "Lua", version = "5.1"}
string.gsub("$name-$version", "$(%w+)", t)  --> "Lua-5.1", 2

-- With function
string.gsub("hello", ".", function(c) return c:upper() end)  --> "HELLO", 5
```

#### `string.len(s)`

Returns length of string.

#### `string.lower(s)`

Returns lowercase string.

#### `string.match(s, pattern [, init])`

Returns captures from first match, or whole match if no captures.

```lua
string.match("hello", "e..")      --> "ell"
string.match("key=val", "(%w+)=(%w+)")  --> "key", "val"
```

#### `string.rep(s, n)`

Returns string repeated n times.

```lua
string.rep("ab", 3)  --> "ababab"
```

#### `string.reverse(s)`

Returns reversed string.

#### `string.sub(s, i [, j])`

Returns substring. Default j=-1 (end of string).

```lua
string.sub("hello", 2, 4)   --> "ell"
string.sub("hello", 2)      --> "ello"
string.sub("hello", -2)     --> "lo"
string.sub("hello", 1, -2)  --> "hell"
```

#### `string.upper(s)`

Returns uppercase string.

______________________________________________________________________

### 3.5 Table Library

#### `table.concat(table [, sep [, i [, j]]])`

Concatenates array elements into string.

```lua
table.concat({1, 2, 3})         --> "123"
table.concat({1, 2, 3}, ", ")   --> "1, 2, 3"
table.concat({1, 2, 3}, ", ", 2, 3)  --> "2, 3"
```

#### `table.insert(table, [pos,] value)`

Inserts value into table at position (default: end). Shifts elements up.

```lua
local t = {1, 2, 3}
table.insert(t, 4)      -- t = {1, 2, 3, 4}
table.insert(t, 2, 10)  -- t = {1, 10, 2, 3, 4}
```

#### `table.maxn(table)`

Returns largest positive numeric index (even with holes). Deprecated in later versions.

```lua
table.maxn({[1]=1, [100]=2})  --> 100
```

#### `table.remove(table [, pos])`

Removes and returns element at position (default: last). Shifts elements down.

```lua
local t = {1, 2, 3, 4}
table.remove(t)     --> 4, t = {1, 2, 3}
table.remove(t, 1)  --> 1, t = {2, 3}
```

#### `table.sort(table [, comp])`

Sorts table in place. Uses `<` by default, or custom comparator.

```lua
local t = {3, 1, 2}
table.sort(t)  -- t = {1, 2, 3}

local t = {{n=2}, {n=1}, {n=3}}
table.sort(t, function(a, b) return a.n < b.n end)
```

______________________________________________________________________

### 3.6 Math Library

#### Constants

```lua
math.huge   -- Infinity (inf)
math.pi     -- 3.14159265358979...
```

#### Trigonometric Functions (radians)

```lua
math.sin(x)      -- Sine
math.cos(x)      -- Cosine
math.tan(x)      -- Tangent
math.asin(x)     -- Arc sine (-1 to 1)
math.acos(x)     -- Arc cosine (-1 to 1)
math.atan(x)     -- Arc tangent
math.atan2(y, x) -- Arc tangent of y/x (handles quadrants)
math.sinh(x)     -- Hyperbolic sine
math.cosh(x)     -- Hyperbolic cosine
math.tanh(x)     -- Hyperbolic tangent
```

#### Exponential and Logarithmic

```lua
math.exp(x)       -- e^x
math.log(x)       -- Natural logarithm
math.log10(x)     -- Base-10 logarithm
math.pow(x, y)    -- x^y (same as x^y operator)
math.sqrt(x)      -- Square root
```

#### Rounding and Remainder

```lua
math.floor(x)     -- Round down to integer
math.ceil(x)      -- Round up to integer
math.abs(x)       -- Absolute value
math.fmod(x, y)   -- Remainder of x/y (same sign as x)
math.modf(x)      -- Returns integer part, fractional part
```

#### Comparison

```lua
math.min(x, ...)  -- Minimum value
math.max(x, ...)  -- Maximum value
```

#### Random Numbers

```lua
math.random()       -- Random float in [0,1)
math.random(n)      -- Random integer in [1,n]
math.random(m, n)   -- Random integer in [m,n]
math.randomseed(x)  -- Set random seed
```

#### Other

```lua
math.deg(x)       -- Radians to degrees
math.rad(x)       -- Degrees to radians
math.frexp(x)     -- Returns mantissa, exponent (x = m * 2^e)
math.ldexp(m, e)  -- Returns m * 2^e
```

______________________________________________________________________

### 3.7 I/O Library

Two models:

1. **Simple model** - Uses implicit current input/output files
1. **Complete model** - Uses explicit file handles

#### Simple Model

```lua
io.input([file])    -- Set/get current input file
io.output([file])   -- Set/get current output file
io.read(...)        -- Read from current input
io.write(...)       -- Write to current output
io.lines([file])    -- Iterator over lines
io.flush()          -- Flush current output
io.close([file])    -- Close file (default: current output)
```

#### Opening Files

```lua
io.open(filename [, mode])
```

Modes:

- `"r"` - Read (default)
- `"w"` - Write (truncate)
- `"a"` - Append
- `"r+"` - Read/update
- `"w+"` - Write/update (truncate)
- `"a+"` - Append/update
- Add `"b"` for binary mode (e.g., `"rb"`)

Returns file handle, or nil + error message.

#### File Handle Methods

```lua
file:read(...)      -- Read formats: "*n", "*a", "*l", number
file:write(...)     -- Write strings/numbers
file:lines()        -- Iterator over lines
file:seek([whence [, offset]])  -- Set/get position
file:setvbuf(mode [, size])     -- Set buffering
file:flush()        -- Flush buffer
file:close()        -- Close file
```

**Read Formats**:

- `"*n"` - Read number
- `"*a"` - Read all (entire file)
- `"*l"` - Read line without newline (default)
- `"*L"` - Read line with newline
- `number` - Read up to n bytes

**Seek Whence**:

- `"set"` - From beginning
- `"cur"` - From current position (default)
- `"end"` - From end

**Buffer Modes**:

- `"no"` - No buffering
- `"full"` - Full buffering
- `"line"` - Line buffering

#### Standard Files

```lua
io.stdin   -- Standard input
io.stdout  -- Standard output
io.stderr  -- Standard error
```

#### Other Functions

```lua
io.popen(prog [, mode])  -- Open process for reading/writing
io.tmpfile()             -- Create temporary file
io.type(obj)             -- Returns "file", "closed file", or nil
```

______________________________________________________________________

### 3.8 OS Library

#### `os.clock()`

Returns CPU time used (seconds).

#### `os.date([format [, time]])`

Returns date/time string or table.

```lua
os.date()                    -- Current date/time string
os.date("%Y-%m-%d %H:%M:%S") -- "2024-01-15 14:30:00"
os.date("*t")                -- Table with fields
os.date("!*t")               -- UTC table
os.date("%c", time)          -- Format specific time
```

Format codes:

- `%a` - Abbreviated weekday
- `%A` - Full weekday
- `%b` - Abbreviated month
- `%B` - Full month
- `%c` - Date and time
- `%d` - Day of month (01-31)
- `%H` - Hour (00-23)
- `%I` - Hour (01-12)
- `%j` - Day of year (001-366)
- `%m` - Month (01-12)
- `%M` - Minute (00-59)
- `%p` - AM/PM
- `%S` - Second (00-60)
- `%w` - Weekday (0-6, Sunday=0)
- `%W` - Week number (00-53)
- `%x` - Date
- `%X` - Time
- `%y` - Year (00-99)
- `%Y` - Year (4 digits)
- `%z` - Timezone offset
- `%%` - Literal %

Table fields: `year`, `month`, `day`, `hour`, `min`, `sec`, `wday`, `yday`, `isdst`

#### `os.difftime(t2, t1)`

Returns difference in seconds.

#### `os.execute([command])`

Runs shell command. Returns status code.

#### `os.exit([code])`

Terminates program. Default code=0 (success).

#### `os.getenv(varname)`

Returns environment variable value.

#### `os.remove(filename)`

Deletes file. Returns true, or nil + error.

#### `os.rename(oldname, newname)`

Renames file. Returns true, or nil + error.

#### `os.setlocale(locale [, category])`

Sets locale. Categories: "all", "collate", "ctype", "monetary", "numeric", "time".

#### `os.time([table])`

Returns current time as number, or time from table.

```lua
os.time()                               -- Current timestamp
os.time({year=2024, month=1, day=15})   -- Specific date
```

#### `os.tmpname()`

Returns temporary filename.

______________________________________________________________________

### 3.9 Debug Library

#### `debug.debug()`

Enters interactive debug mode. Read-eval-print loop.

#### `debug.getfenv(o)`

Returns environment of object.

#### `debug.gethook([thread])`

Returns current hook settings: hook function, mask string, count.

#### `debug.getinfo(thread, function [, what])`

Returns table with function info. `what` selects fields:

- `"n"` - `name`, `namewhat`
- `"S"` - `source`, `short_src`, `what`, `linedefined`, `lastlinedefined`
- `"l"` - `currentline`
- `"u"` - `nups` (number of upvalues)
- `"f"` - `func` (the function itself)
- `"L"` - `activelines` (table of valid lines)

#### `debug.getlocal([thread,] level, local)`

Returns name and value of local variable.

#### `debug.getmetatable(object)`

Returns metatable (bypasses `__metatable`).

#### `debug.getregistry()`

Returns registry table.

#### `debug.getupvalue(func, up)`

Returns name and value of upvalue.

#### `debug.setfenv(object, table)`

Sets environment.

#### `debug.sethook([thread,] hook, mask [, count])`

Sets debug hook. Mask is string with:

- `"c"` - Call events
- `"r"` - Return events
- `"l"` - Line events

#### `debug.setlocal([thread,] level, local, value)`

Sets local variable value.

#### `debug.setmetatable(object, table)`

Sets metatable (bypasses `__metatable`).

#### `debug.setupvalue(func, up, value)`

Sets upvalue value.

#### `debug.traceback([thread,] [message [, level]])`

Returns stack traceback string.

```lua
debug.traceback()
debug.traceback("Error occurred")
debug.traceback("Error", 2)  -- Start from level 2
```

______________________________________________________________________

## 4. Metamethods Reference

### Arithmetic Metamethods

| Metamethod | Operator | Signature                          |
| ---------- | -------- | ---------------------------------- |
| `__add`    | `a + b`  | `function(a, b) return result end` |
| `__sub`    | `a - b`  | `function(a, b) return result end` |
| `__mul`    | `a * b`  | `function(a, b) return result end` |
| `__div`    | `a / b`  | `function(a, b) return result end` |
| `__mod`    | `a % b`  | `function(a, b) return result end` |
| `__pow`    | `a ^ b`  | `function(a, b) return result end` |
| `__unm`    | `-a`     | `function(a) return result end`    |
| `__concat` | `a .. b` | `function(a, b) return result end` |

**Lookup Order**: Check left operand's metatable, then right operand's metatable.

### Relational Metamethods

| Metamethod | Operator | Notes                                                             |
| ---------- | -------- | ----------------------------------------------------------------- |
| `__eq`     | `a == b` | Only called if both operands are same type and have same `__eq`   |
| `__lt`     | `a < b`  | Also used for `a > b` (as `b < a`)                                |
| `__le`     | `a <= b` | Also used for `a >= b` (as `b <= a`). Falls back to `not (b < a)` |

### Table Access Metamethods

#### `__index`

Called when accessing a nil field. Can be function or table.

```lua
-- As function
mt.__index = function(table, key)
    return "default"
end

-- As table (fallback lookup)
mt.__index = defaults_table
```

#### `__newindex`

Called when assigning to a nil field.

```lua
mt.__newindex = function(table, key, value)
    rawset(table, key, value)  -- Use rawset to avoid recursion
end
```

### Other Metamethods

| Metamethod    | Purpose                                                                                        |
| ------------- | ---------------------------------------------------------------------------------------------- |
| `__call`      | Called when table is called as function: `t()`                                                 |
| `__tostring`  | Called by `tostring()`                                                                         |
| `__metatable` | Returned by `getmetatable()` instead of real metatable. If set, `setmetatable()` raises error. |
| `__mode`      | Weak table mode: `"k"`, `"v"`, or `"kv"`                                                       |
| `__len`       | **NOT in 5.1** - Length operator `#` doesn't use metamethods for tables                        |
| `__gc`        | Finalizer for userdata (not tables in 5.1)                                                     |

### Example: Proxy Table

```lua
local function readonly(t)
    local proxy = {}
    local mt = {
        __index = t,
        __newindex = function(t, k, v)
            error("attempt to modify read-only table", 2)
        end,
        __pairs = function() return pairs(t) end,
        __len = function() return #t end,
        __metatable = "protected"
    }
    return setmetatable(proxy, mt)
end
```

______________________________________________________________________

## 5. String Patterns

Lua patterns are not full regular expressions but cover common needs.

### Character Classes

| Pattern | Description                                      |
| ------- | ------------------------------------------------ |
| `.`     | Any character                                    |
| `%a`    | Letter (A-Za-z)                                  |
| `%c`    | Control character                                |
| `%d`    | Digit (0-9)                                      |
| `%g`    | Printable character except space                 |
| `%l`    | Lowercase letter                                 |
| `%p`    | Punctuation                                      |
| `%s`    | Whitespace                                       |
| `%u`    | Uppercase letter                                 |
| `%w`    | Alphanumeric (A-Za-z0-9)                         |
| `%x`    | Hexadecimal digit                                |
| `%z`    | Character with representation 0 (null)           |
| `%X`    | Uppercase = complement (e.g., `%A` = non-letter) |

### Magic Characters

These have special meaning and must be escaped with `%`:

```
( ) . % + - * ? [ ^ $
```

To match literal `%`, use `%%`.

### Pattern Items

| Pattern  | Description                                      |
| -------- | ------------------------------------------------ |
| `x`      | Matches character x literally                    |
| `.`      | Matches any character                            |
| `%x`     | Matches character class or escaped magic         |
| `[set]`  | Character set (e.g., `[abc]`, `[a-z]`, `[^0-9]`) |
| `[^set]` | Complement of set                                |

### Quantifiers

| Quantifier | Description                 |
| ---------- | --------------------------- |
| `*`        | 0 or more (greedy)          |
| `+`        | 1 or more (greedy)          |
| `-`        | 0 or more (non-greedy/lazy) |
| `?`        | 0 or 1                      |

### Anchors

| Anchor | Description                             |
| ------ | --------------------------------------- |
| `^`    | Start of string (only at pattern start) |
| `$`    | End of string (only at pattern end)     |

### Captures

| Pattern     | Description                                  |
| ----------- | -------------------------------------------- |
| `(pattern)` | Capture matched substring                    |
| `%n`        | Match nth capture (backreference, n=1-9)     |
| `%bxy`      | Balanced pair starting with x, ending with y |

### Examples

```lua
-- Match email-like pattern
"([%w._]+)@([%w._]+)"

-- Match quoted string
'"([^"]*)"'

-- Match balanced parentheses
"%b()"

-- Match word boundaries (workaround - no \b)
"%f[%w]word%f[%W]"  -- frontier pattern (5.1+ unofficial)

-- Replace with captures
string.gsub("hello world", "(%w+)", "[%1]")  --> "[hello] [world]"

-- Non-greedy matching
string.match("<tag>content</tag>", "<(.-)>")  --> "tag"
string.match("<tag>content</tag>", "<(.+)>")  --> "tag>content</tag" (greedy)
```

______________________________________________________________________

## 6. Operator Precedence

From lowest to highest:

| Precedence  | Operators              | Associativity |
| ----------- | ---------------------- | ------------- |
| 1 (lowest)  | `or`                   | Left          |
| 2           | `and`                  | Left          |
| 3           | `<  >  <=  >=  ~=  ==` | Left          |
| 4           | `..`                   | Right         |
| 5           | `+  -`                 | Left          |
| 6           | `*  /  %`              | Left          |
| 7           | `not  #  -` (unary)    | Unary         |
| 8 (highest) | `^`                    | Right         |

**Note**: `^` and `..` are right-associative. All others are left-associative.

```lua
-- Precedence examples
2 + 3 * 4       --> 14 (not 20)
2 ^ 3 ^ 2       --> 512 (2^9, not 64)
"a" .. "b" .. "c"  --> "abc"
not not true    --> true
```

______________________________________________________________________

## Appendix: Differences from Lua 5.0

Major changes in 5.1:

1. **Module system**: New `require`, `module`, `package` table
1. **Long strings**: `[[string]]` no longer nests; use `[=[string]=]`
1. **`arg` table**: Vararg `...` replaces implicit `arg` in functions
1. **`#` operator**: New length operator (was `table.getn`)
1. **`table.setn`**: Removed
1. **`table.foreach/foreachi`**: Deprecated (use `pairs`/`ipairs`)
1. **`gcinfo`**: Deprecated (use `collectgarbage("count")`)
1. **Weak table changes**: Keys with finalizers are now weak
1. **Incremental GC**: Replaces stop-the-world collector

______________________________________________________________________

## References

- Official Lua 5.1 Reference Manual: https://www.lua.org/manual/5.1/
- Lua 5.1 Source Code: https://www.lua.org/ftp/lua-5.1.5.tar.gz
- Programming in Lua, 2nd Edition (for Lua 5.1): https://www.lua.org/pil/
