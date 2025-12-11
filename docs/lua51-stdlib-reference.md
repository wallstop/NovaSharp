# Lua 5.1 Standard Library Reference

**Note:** This document supplements the main Lua 5.1 reference with detailed standard library function signatures. While compiled from authoritative Lua knowledge, please verify critical details against the official manual at https://www.lua.org/manual/5.1/manual.html

______________________________________________________________________

## Table of Contents

- [5.1 Basic Functions](#51-basic-functions)
- [5.2 Coroutine Manipulation](#52-coroutine-manipulation)
- [5.3 Modules (Package Library)](#53-modules-package-library)
- [5.4 String Manipulation](#54-string-manipulation)
- [5.5 Table Manipulation](#55-table-manipulation)
- [5.6 Mathematical Functions](#56-mathematical-functions)
- [5.7 Input and Output Facilities](#57-input-and-output-facilities)
- [5.8 Operating System Facilities](#58-operating-system-facilities)
- [5.9 The Debug Library](#59-the-debug-library)

______________________________________________________________________

## 5.1 Basic Functions

The basic library provides core functions available in the global environment.

### assert

```lua
assert(v [, message])
```

Issues an error when the value of `v` is false (i.e., nil or false); otherwise returns all its arguments. `message` is an error message; defaults to "assertion failed!"

**Example:**

```lua
assert(type(x) == "number", "x must be a number")
```

### collectgarbage

```lua
collectgarbage([opt [, arg]])
```

Generic interface to the garbage collector. Performs different functions according to its first argument, `opt`:

- `"collect"` - performs a full garbage-collection cycle (default)
- `"stop"` - stops the garbage collector
- `"restart"` - restarts the garbage collector
- `"count"` - returns memory usage in kilobytes
- `"step"` - performs a garbage-collection step (size specified by `arg`)
- `"setpause"` - sets `arg` as the new pause value (returns previous value)
- `"setstepmul"` - sets `arg` as the new step multiplier (returns previous value)

**Returns:** Depends on option; `"count"` returns current memory in KB and fractional part.

### dofile

```lua
dofile([filename])
```

Opens the named file and executes its contents as a Lua chunk. If called without arguments, executes the contents of standard input (`stdin`). Returns all values returned by the chunk. In case of errors, propagates the error to its caller (does not run in protected mode).

### error

```lua
error(message [, level])
```

Terminates the last protected function called and returns `message` as the error message. `error` never returns.

The `level` argument specifies how to get the error position:

- `level = 1` (default) - error position is where `error` was called
- `level = 2` - error position is where the function that called `error` was called
- `level = 0` - no error position information is added

### \_G

```lua
_G
```

A global variable (not a function) that holds the global environment. Lua itself does not use this variable; changing its value does not affect any environment. `_G._G == _G` is always true.

### getfenv

```lua
getfenv([f])
```

Returns the current environment in use by the function `f`.

- `f` can be a function or a number (stack level: 1 = calling function, 2 = function that called the caller, etc.)
- If `f` is 0, returns the global environment
- Default is 1 (calling function's environment)

### getmetatable

```lua
getmetatable(object)
```

If `object` does not have a metatable, returns nil. Otherwise:

- If the metatable has a `__metatable` field, returns that value
- Otherwise, returns the metatable of the given object

### ipairs

```lua
ipairs(t)
```

Returns three values: an iterator function, the table `t`, and 0, so that:

```lua
for i, v in ipairs(t) do body end
```

iterates over the pairs `(1, t[1])`, `(2, t[2])`, ..., up to the first nil value.

### load

```lua
load(func [, chunkname])
```

Loads a chunk using function `func` to get its pieces. Each call to `func` must return a string that concatenates with previous results. A return of nil (or no value) signals the end of the chunk.

Returns the compiled chunk as a function, or nil plus an error message if there are errors.

`chunkname` is used in error messages and debug information (defaults to "=(load)").

### loadfile

```lua
loadfile([filename])
```

Similar to `load`, but gets the chunk from file `filename` or from standard input if no filename is given.

Returns the compiled chunk as a function, or nil plus an error message if there are errors.

### loadstring

```lua
loadstring(string [, chunkname])
```

Similar to `load`, but gets the chunk from the given string.

Returns the compiled chunk as a function, or nil plus an error message if there are syntax errors.

To load and run a string, use:

```lua
assert(loadstring(s))()
```

`chunkname` is used for error messages and debug info (defaults to the string itself).

### next

```lua
next(table [, index])
```

Allows traversal of all fields of a table. Returns the next index of the table and its associated value.

When called with nil as second argument, returns the first index and its value. When called with the last index, or with nil in an empty table, returns nil.

If `index` is not a valid key (or nil), behavior is undefined.

**Example:**

```lua
for k, v in next, t do
    -- process k, v
end
```

### pairs

```lua
pairs(t)
```

Returns three values: the `next` function, the table `t`, and nil, so that:

```lua
for k, v in pairs(t) do body end
```

iterates over all key-value pairs of table `t`.

### pcall

```lua
pcall(f, arg1, ...)
```

Calls function `f` with the given arguments in protected mode. Any errors are caught and returned.

**Returns:**

- On success: `true` followed by all results from the call
- On error: `false` followed by the error message

### print

```lua
print(...)
```

Receives any number of arguments and prints their values to stdout, using `tostring` to convert each argument to a string. Not intended for formatted output; use `string.format` for that.

### rawequal

```lua
rawequal(v1, v2)
```

Checks whether `v1` equals `v2`, without invoking any metamethod. Returns a boolean.

### rawget

```lua
rawget(table, index)
```

Gets the real value of `table[index]`, without invoking any metamethod. `table` must be a table; `index` may be any value.

### rawset

```lua
rawset(table, index, value)
```

Sets the real value of `table[index]` to `value`, without invoking any metamethod. `table` must be a table, `index` any value different from nil, and `value` any Lua value.

Returns `table`.

### select

```lua
select(index, ...)
```

If `index` is a number, returns all arguments after argument number `index`. Otherwise, `index` must be the string `"#"`, and returns the total number of extra arguments it received.

**Examples:**

```lua
select(2, "a", "b", "c")  --> "b"  "c"
select("#", "a", "b", "c") --> 3
```

### setfenv

```lua
setfenv(f, table)
```

Sets the environment to be used by the given function.

- `f` can be a function or a number (stack level)
- `table` must be a table

Returns the function.

### setmetatable

```lua
setmetatable(table, metatable)
```

Sets the metatable for the given table. If `metatable` is nil, removes the metatable. If the original metatable has a `__metatable` field, raises an error.

Returns `table`.

### tonumber

```lua
tonumber(e [, base])
```

Tries to convert its argument to a number.

- If already a number or convertible string, returns the number
- Otherwise returns nil

Optional `base` (between 2 and 36) specifies the base to interpret the numeral. In bases above 10, letter 'A' represents 10, 'B' represents 11, ..., 'Z' represents 35.

**Examples:**

```lua
tonumber("10")      --> 10
tonumber("10", 2)   --> 2
tonumber("ff", 16)  --> 255
tonumber("1000", 2) --> 8
```

### tostring

```lua
tostring(v)
```

Receives an argument of any type and converts it to a string in a reasonable format.

For complete control of how numbers are converted, use `string.format`.

If the metatable has a `__tostring` field, `tostring` calls it with the value as argument and uses the result.

### type

```lua
type(v)
```

Returns the type of its only argument, coded as a string. Possible results:

- `"nil"`
- `"number"`
- `"string"`
- `"boolean"`
- `"table"`
- `"function"`
- `"thread"`
- `"userdata"`

### unpack

```lua
unpack(list [, i [, j]])
```

Returns the elements from the given table. Roughly equivalent to:

```lua
return list[i], list[i+1], ..., list[j]
```

Defaults: `i = 1`, `j = #list`.

### \_VERSION

```lua
_VERSION
```

A global variable (not a function) containing a string with the current interpreter version. Current content for Lua 5.1: `"Lua 5.1"`.

### xpcall

```lua
xpcall(f, err)
```

Similar to `pcall`, but sets a new error handler `err`.

The error handler is called with the original error message and can return a new error message. It is called before the stack unwinds, so it can gather more information using the debug library.

**Returns:** Same as `pcall` - status code plus results or error.

______________________________________________________________________

## 5.2 Coroutine Manipulation

The coroutine library comprises functions to manipulate coroutines.

### coroutine.create

```lua
coroutine.create(f)
```

Creates a new coroutine with body function `f`. Returns a coroutine object (a thread). The coroutine starts in suspended state.

### coroutine.resume

```lua
coroutine.resume(co [, val1, ...])
```

Starts or continues the execution of coroutine `co`.

The first time you resume a coroutine, it starts running its body. Arguments `val1, ...` are passed as arguments to the body function.

If the coroutine has yielded, `resume` restarts it, with values `val1, ...` being returned by the `yield` call.

**Returns:**

- On success: `true` plus any values passed to `yield` (or returned by body function)
- On error: `false` plus the error message

### coroutine.running

```lua
coroutine.running()
```

Returns the running coroutine, or nil when called by the main thread.

### coroutine.status

```lua
coroutine.status(co)
```

Returns the status of coroutine `co` as a string:

- `"running"` - the coroutine is running (it's the one calling `status`)
- `"suspended"` - the coroutine is suspended (in a call to `yield`) or not started yet
- `"normal"` - the coroutine is active but not running (it has resumed another coroutine)
- `"dead"` - the coroutine has finished or stopped with an error

### coroutine.wrap

```lua
coroutine.wrap(f)
```

Creates a new coroutine with body `f`. Returns a function that resumes the coroutine each time it is called.

Any arguments passed to the function behave as extra arguments to `resume`. Returns the same values returned by `resume`, except the first boolean.

Unlike `resume`, errors are not caught and propagate to the caller.

### coroutine.yield

```lua
coroutine.yield(...)
```

Suspends execution of the calling coroutine. Any arguments to `yield` are passed as extra results to `resume`.

______________________________________________________________________

## 5.3 Modules (Package Library)

The package library provides basic facilities for loading and managing modules.

### module

```lua
module(name [, ...])
```

Creates a module. If there is a table in `package.loaded[name]`, this table is the module. Otherwise creates a new table and sets it into `package.loaded[name]` and the global `name`.

Sets `_M` to the module table, `_NAME` to `name`, and `_PACKAGE` to the package name (full module name minus the last component).

### require

```lua
require(modname)
```

Loads the given module. First checks `package.loaded[modname]` to determine if the module is already loaded. If not, searches for a loader using the paths in `package.path` (for Lua files) and `package.cpath` (for C libraries).

Returns the value returned by the loader (or `true` if no return value), which is also stored in `package.loaded[modname]`.

### package.cpath

```lua
package.cpath
```

The path used by `require` to search for C loaders. Uses the same format as `package.path`, but with platform-specific library extensions (`.dll` on Windows, `.so` on Unix).

### package.loaded

```lua
package.loaded
```

A table used by `require` to control which modules are already loaded. When `require` is called with module name `modname` and `package.loaded[modname]` is not false, returns that value without loading the module.

### package.loadlib

```lua
package.loadlib(libname, funcname)
```

Dynamically links the host program with the C library `libname`. Searches for the function `funcname` inside the library and returns it as a C function.

This is a low-level function; normally use `require`.

### package.path

```lua
package.path
```

The path used by `require` to search for Lua loaders. At start-up, Lua initializes this variable with the value of environment variable `LUA_PATH` or a default path.

Path components are separated by semicolons. Each component can contain `?` which is replaced by the module name.

**Example path:** `"./?.lua;/usr/local/lua/?.lua"`

### package.preload

```lua
package.preload
```

A table to store loaders for specific modules. When searching for a module, `require` checks this table first. If `package.preload[modname]` contains a function, that function is used as the loader.

### package.seeall

```lua
package.seeall(module)
```

Sets a metatable for `module` with its `__index` field referring to the global environment, so the module inherits values from the global environment.

______________________________________________________________________

## 5.4 String Manipulation

The string library provides functions for string manipulation.

**Note:** Strings in Lua are indexed from 1, not 0. Indices can be negative, counting from the end: -1 is the last character, -2 is second-to-last, etc.

### string.byte

```lua
string.byte(s [, i [, j]])
```

Returns the internal numerical codes of characters `s[i]`, `s[i+1]`, ..., `s[j]`. Default for `i` is 1; default for `j` is `i`.

**Example:**

```lua
string.byte("ABC")      --> 65
string.byte("ABC", 2)   --> 66
string.byte("ABC", 1, 3) --> 65  66  67
```

### string.char

```lua
string.char(...)
```

Receives zero or more integers and returns a string with length equal to the number of arguments, in which each character has the internal numerical code equal to its corresponding argument.

**Example:**

```lua
string.char(65, 66, 67)  --> "ABC"
```

### string.dump

```lua
string.dump(function)
```

Returns a string containing a binary representation (a binary chunk) of the given function, so that a later `loadstring` on this string returns a copy of the function.

### string.find

```lua
string.find(s, pattern [, init [, plain]])
```

Looks for the first match of `pattern` in string `s`.

If found, returns the indices of where the match starts and ends; otherwise returns nil.

A third, optional numerical argument `init` specifies where to start the search (default is 1, can be negative).

A fourth optional argument `plain` turns off pattern matching, making the function do a plain "find substring" operation.

If the pattern has captures, returns the captured values after the two indices.

**Examples:**

```lua
string.find("hello world", "world")     --> 7  11
string.find("hello world", "w..ld")     --> 7  11
string.find("hello world", "planet")    --> nil
string.find("hello world", "o", 5)      --> 8  8
```

### string.format

```lua
string.format(formatstring, ...)
```

Returns a formatted version of its variable number of arguments following the description given in its first argument (which must be a string).

The format string follows the same rules as the C function `sprintf`. Options:

- `%c` - character
- `%d`, `%i` - signed decimal integer
- `%o` - unsigned octal
- `%u` - unsigned decimal integer
- `%x`, `%X` - unsigned hexadecimal integer
- `%e`, `%E` - scientific notation
- `%f` - decimal floating point
- `%g`, `%G` - shorter of `%e` or `%f`
- `%q` - formats string for safe reading by Lua interpreter
- `%s` - string
- `%%` - literal percent sign

**Examples:**

```lua
string.format("x = %d", 10)           --> "x = 10"
string.format("%x", 255)              --> "ff"
string.format("%5.2f", 3.14159)       --> " 3.14"
string.format("Hello %s", "World")    --> "Hello World"
```

### string.gmatch

```lua
string.gmatch(s, pattern)
```

Returns an iterator function that, each time it is called, returns the next captures from `pattern` over string `s`.

If `pattern` specifies no captures, the whole match is produced in each call.

**Example:**

```lua
s = "hello world from Lua"
for w in string.gmatch(s, "%a+") do
    print(w)
end
-- Prints: hello, world, from, Lua
```

### string.gsub

```lua
string.gsub(s, pattern, repl [, n])
```

Returns a copy of `s` in which all (or the first `n`, if given) occurrences of `pattern` have been replaced by `repl`.

`repl` can be:

- A string: replacements are made literally (the character `%` works as escape)
  - `%0` - whole match
  - `%1`, `%2`, ... - captures
  - `%%` - literal `%`
- A table: first capture is looked up as key in table
- A function: called with all captured substrings (or whole match if no captures)

**Returns:** Modified string and the number of substitutions made.

**Examples:**

```lua
string.gsub("hello world", "(%w+)", "%1 %1")
  --> "hello hello world world", 2

string.gsub("hello world", "%w+", "%0 %0", 1)
  --> "hello hello world", 1

string.gsub("hello world from Lua", "(%w+)%s*(%w+)", "%2 %1")
  --> "world hello Lua from", 2
```

### string.len

```lua
string.len(s)
```

Returns the length of string `s` (number of bytes).

Empty string has length 0. Embedded zeros are counted.

### string.lower

```lua
string.lower(s)
```

Returns a copy of string `s` with all uppercase letters changed to lowercase. All other characters are left unchanged.

### string.match

```lua
string.match(s, pattern [, init])
```

Looks for the first match of `pattern` in string `s`. If found, returns the captures from the pattern; otherwise returns nil.

If `pattern` specifies no captures, the whole match is returned.

Optional `init` specifies where to start search (default 1).

**Examples:**

```lua
string.match("I have 2 apples", "%d+")  --> "2"
string.match("hello", "(%w+)")          --> "hello"
```

### string.rep

```lua
string.rep(s, n)
```

Returns a string that is the concatenation of `n` copies of string `s`.

**Example:**

```lua
string.rep("ab", 3)  --> "ababab"
```

### string.reverse

```lua
string.reverse(s)
```

Returns a string that is the string `s` reversed.

**Example:**

```lua
string.reverse("hello")  --> "olleh"
```

### string.sub

```lua
string.sub(s, i [, j])
```

Returns the substring of `s` that starts at `i` and continues until `j`. Both `i` and `j` can be negative.

If `j` is absent, it defaults to -1 (end of string).

**Examples:**

```lua
string.sub("hello", 2)      --> "ello"
string.sub("hello", 2, 4)   --> "ell"
string.sub("hello", -2)     --> "lo"
```

### string.upper

```lua
string.upper(s)
```

Returns a copy of string `s` with all lowercase letters changed to uppercase. All other characters are left unchanged.

______________________________________________________________________

### String Pattern Matching

Lua patterns are a powerful but simpler alternative to regular expressions. Patterns are strings used to describe sets of strings using special characters.

#### Character Classes

A character class is used to represent a set of characters:

- `.` - all characters
- `%a` - all letters
- `%c` - all control characters
- `%d` - all digits
- `%l` - all lowercase letters
- `%p` - all punctuation characters
- `%s` - all space characters
- `%u` - all uppercase letters
- `%w` - all alphanumeric characters
- `%x` - all hexadecimal digits
- `%z` - the null character (ASCII 0)

Uppercase versions of these classes (`%A`, `%C`, etc.) represent the complement of the class.

#### Magic Characters

The following characters have special meaning in patterns (magic characters):

```
( ) . % + - * ? [ ] ^ $
```

To match these characters literally, precede them with `%`:

```lua
string.find("a.b", "%.")  -- finds the dot
```

#### Pattern Items

- **Character class:** matches any single character in the class
- **Character class with `*`:** matches 0 or more repetitions (longest match)
- **Character class with `+`:** matches 1 or more repetitions (longest match)
- **Character class with `-`:** matches 0 or more repetitions (shortest match)
- **Character class with `?`:** matches 0 or 1 occurrence (optional)
- **`%n`** (where n is 1-9): matches a substring equal to the n-th captured string
- **`%bxy`:** matches balanced pairs (x and y are distinct characters)
  - Example: `%b()` matches balanced parentheses
- **`[set]`:** character set (e.g., `[abc]` matches a, b, or c)
- **`[^set]`:** complement of character set
- **`^pattern`:** anchors match to beginning of string
- **`pattern$`:** anchors match to end of string

#### Captures

A pattern can contain sub-patterns enclosed in parentheses; these describe captures. When a match succeeds, the substrings that match captures are stored for future use.

**Examples:**

```lua
-- Capture date parts
string.match("Today is 17/05/2023", "(%d+)/(%d+)/(%d+)")
  --> "17"  "05"  "2023"

-- Capture word boundaries
string.match("hello world", "(%a+)%s+(%a+)")
  --> "hello"  "world"

-- Empty captures: `()` captures current position (a number)
string.match("flaaap", "()aa()")  --> 3  5
```

______________________________________________________________________

## 5.5 Table Manipulation

The table library provides functions to manipulate tables as arrays.

**Note:** All functions ignore non-numeric keys. Array functions assume tables are sequences (no holes).

### table.concat

```lua
table.concat(table [, sep [, i [, j]]])
```

Given an array where all elements are strings or numbers, returns `table[i]..sep..table[i+1] ... sep..table[j]`.

Default for `sep` is empty string, for `i` is 1, and for `j` is `#table`.

**Example:**

```lua
table.concat({"a", "b", "c"}, ", ")  --> "a, b, c"
table.concat({10, 20, 30}, "-")      --> "10-20-30"
```

### table.insert

```lua
table.insert(table, [pos,] value)
```

Inserts element `value` at position `pos` in `table`, shifting up other elements to open space.

Default for `pos` is `#table + 1`, so `table.insert(t, x)` inserts at the end.

**Examples:**

```lua
t = {10, 20, 30}
table.insert(t, 40)     -- t = {10, 20, 30, 40}
table.insert(t, 2, 15)  -- t = {10, 15, 20, 30, 40}
```

### table.maxn

```lua
table.maxn(table)
```

Returns the largest positive numerical index of the given table, or zero if the table has no positive numerical indices.

(To do its job, this function does a linear traversal of the whole table.)

### table.remove

```lua
table.remove(table [, pos])
```

Removes from `table` the element at position `pos`, shifting down other elements to close the space.

Default for `pos` is `#table`, so `table.remove(t)` removes the last element.

Returns the removed element.

**Examples:**

```lua
t = {10, 20, 30, 40}
table.remove(t)     -- returns 40, t = {10, 20, 30}
table.remove(t, 2)  -- returns 20, t = {10, 30}
```

### table.sort

```lua
table.sort(table [, comp])
```

Sorts table elements in a given order, in-place (modifies the table).

If `comp` is given, it must be a function that receives two elements and returns true when the first is less than the second (so `not comp(a[i+1], a[i])` after the sort).

If `comp` is not given, uses the standard Lua operator `<`.

The sort algorithm is not stable (elements considered equal may have their relative positions changed by the sort).

**Examples:**

```lua
t = {3, 1, 4, 1, 5, 9}
table.sort(t)                    -- t = {1, 1, 3, 4, 5, 9}

table.sort(t, function(a, b) return a > b end)
-- t = {9, 5, 4, 3, 1, 1} (reverse order)
```

______________________________________________________________________

## 5.6 Mathematical Functions

The math library provides mathematical functions.

### Constants

#### math.huge

```lua
math.huge
```

A value larger than any other numerical value (positive infinity).

#### math.pi

```lua
math.pi
```

The value of π (pi).

### math.abs

```lua
math.abs(x)
```

Returns the absolute value of `x`.

### math.acos

```lua
math.acos(x)
```

Returns the arc cosine of `x` (in radians).

### math.asin

```lua
math.asin(x)
```

Returns the arc sine of `x` (in radians).

### math.atan

```lua
math.atan(x)
```

Returns the arc tangent of `x` (in radians).

### math.atan2

```lua
math.atan2(y, x)
```

Returns the arc tangent of `y/x` (in radians), using the signs of both parameters to find the quadrant of the result.

### math.ceil

```lua
math.ceil(x)
```

Returns the smallest integer larger than or equal to `x`.

**Example:**

```lua
math.ceil(3.2)   --> 4
math.ceil(-3.2)  --> -3
```

### math.cos

```lua
math.cos(x)
```

Returns the cosine of `x` (assumed to be in radians).

### math.cosh

```lua
math.cosh(x)
```

Returns the hyperbolic cosine of `x`.

### math.deg

```lua
math.deg(x)
```

Returns the angle `x` (given in radians) in degrees.

### math.exp

```lua
math.exp(x)
```

Returns the value e^x.

### math.floor

```lua
math.floor(x)
```

Returns the largest integer smaller than or equal to `x`.

**Example:**

```lua
math.floor(3.8)   --> 3
math.floor(-3.8)  --> -4
```

### math.fmod

```lua
math.fmod(x, y)
```

Returns the remainder of the division of `x` by `y` that rounds the quotient towards zero.

**Example:**

```lua
math.fmod(10, 3)  --> 1
math.fmod(-10, 3) --> -1
```

### math.frexp

```lua
math.frexp(x)
```

Returns `m` and `e` such that `x = m * 2^e`, where `e` is an integer and the absolute value of `m` is in the range \[0.5, 1) (or zero when `x` is zero).

### math.ldexp

```lua
math.ldexp(m, e)
```

Returns `m * 2^e` (the inverse of `frexp`).

### math.log

```lua
math.log(x)
```

Returns the natural logarithm of `x`.

### math.log10

```lua
math.log10(x)
```

Returns the base-10 logarithm of `x`.

### math.max

```lua
math.max(x, ...)
```

Returns the maximum value among its arguments.

**Example:**

```lua
math.max(1, 5, 3, 9, 2)  --> 9
```

### math.min

```lua
math.min(x, ...)
```

Returns the minimum value among its arguments.

**Example:**

```lua
math.min(1, 5, 3, 9, 2)  --> 1
```

### math.modf

```lua
math.modf(x)
```

Returns two numbers: the integral part of `x` and the fractional part of `x`.

**Example:**

```lua
math.modf(3.14)  --> 3  0.14
math.modf(-3.14) --> -3  -0.14
```

### math.pow

```lua
math.pow(x, y)
```

Returns x^y. (You can also use the expression `x^y` to compute this value.)

### math.rad

```lua
math.rad(x)
```

Returns the angle `x` (given in degrees) in radians.

### math.random

```lua
math.random([m [, n]])
```

Generates pseudo-random numbers.

When called without arguments, returns a uniform pseudo-random real number in the range \[0, 1).

When called with one argument `m`, returns a uniform pseudo-random integer in the range [1, m].

When called with two arguments `m` and `n`, returns a uniform pseudo-random integer in the range [m, n].

**Examples:**

```lua
math.random()      --> 0.0 to 1.0 (e.g., 0.72451)
math.random(10)    --> 1 to 10 (e.g., 7)
math.random(5, 10) --> 5 to 10 (e.g., 8)
```

### math.randomseed

```lua
math.randomseed(x)
```

Sets `x` as the seed for the pseudo-random generator. Equal seeds produce equal sequences of numbers.

### math.sin

```lua
math.sin(x)
```

Returns the sine of `x` (assumed to be in radians).

### math.sinh

```lua
math.sinh(x)
```

Returns the hyperbolic sine of `x`.

### math.sqrt

```lua
math.sqrt(x)
```

Returns the square root of `x`. (You can also use `x^0.5`.)

### math.tan

```lua
math.tan(x)
```

Returns the tangent of `x` (assumed to be in radians).

### math.tanh

```lua
math.tanh(x)
```

Returns the hyperbolic tangent of `x`.

______________________________________________________________________

## 5.7 Input and Output Facilities

The I/O library provides two different styles for file manipulation:

1. **Implicit file descriptors** (using default input/output files)
1. **Explicit file descriptors** (file handles returned by `io.open`)

### io.close

```lua
io.close([file])
```

Closes `file` or the default output file if no file is specified.

Equivalent to `file:close()`.

### io.flush

```lua
io.flush()
```

Flushes the default output file (saves any written data).

### io.input

```lua
io.input([file])
```

When called with a file name, opens the named file (in text mode) and sets its handle as the default input file.

When called with a file handle, sets that handle as the default input file.

When called without parameters, returns the current default input file.

### io.lines

```lua
io.lines([filename])
```

Opens the given file in read mode and returns an iterator function that, each time it is called, returns a new line from the file.

When no more lines, the iterator returns nil and automatically closes the file.

If `filename` is omitted, uses the default input file.

**Example:**

```lua
for line in io.lines("file.txt") do
    print(line)
end
```

### io.open

```lua
io.open(filename [, mode])
```

Opens a file in the specified mode. Returns a file handle or, in case of errors, nil plus an error message.

**Modes:**

- `"r"` - read mode (default)
- `"w"` - write mode
- `"a"` - append mode
- `"r+"` - update mode (read and write)
- `"w+"` - create/truncate and allow read
- `"a+"` - append mode allowing read

Mode string may also have a `b` at the end (e.g., `"rb"`, `"wb"`) to open in binary mode.

### io.output

```lua
io.output([file])
```

Similar to `io.input`, but operates over the default output file.

### io.popen

```lua
io.popen(prog [, mode])
```

Starts program `prog` in a separated process and returns a file handle that can be used to read data from (if `mode` is `"r"`, the default) or write data to (if `mode` is `"w"`) this program.

Not available on all platforms.

### io.read

```lua
io.read(...)
```

Reads from the default input file according to the given formats. For each format, returns a string (or number) with the characters read, or nil if cannot read.

**Formats:**

- `"*n"` - reads a number
- `"*a"` - reads the whole file
- `"*l"` - reads the next line (without newline)
- `"*L"` - reads the next line (with newline)
- `number` - reads a string with up to this number of characters

### io.tmpfile

```lua
io.tmpfile()
```

Returns a handle for a temporary file opened in update mode. The file is automatically removed when the program ends.

### io.type

```lua
io.type(obj)
```

Checks whether `obj` is a valid file handle. Returns:

- `"file"` if `obj` is an open file handle
- `"closed file"` if `obj` is a closed file handle
- `nil` if `obj` is not a file handle

### io.write

```lua
io.write(...)
```

Writes the value of each of its arguments to the default output file. Arguments must be strings or numbers.

Returns the file handle (for chaining calls).

### io.stdin, io.stdout, io.stderr

```lua
io.stdin
io.stdout
io.stderr
```

File handles for standard input, standard output, and standard error.

______________________________________________________________________

### File Methods

When you open a file with `io.open`, you get a file handle object with methods:

#### file:close

```lua
file:close()
```

Closes the file.

#### file:flush

```lua
file:flush()
```

Saves any written data to the file.

#### file:lines

```lua
file:lines()
```

Returns an iterator function that returns a new line each time it is called. Unlike `io.lines`, this does not close the file when loop ends.

#### file:read

```lua
file:read(...)
```

Reads from the file according to the given formats (same as `io.read`).

#### file:seek

```lua
file:seek([whence [, offset]])
```

Sets and gets the file position, measured from the beginning of the file.

**whence:**

- `"set"` - base is beginning of file (default)
- `"cur"` - base is current position
- `"end"` - base is end of file

**Returns:** The final file position measured from the beginning.

#### file:setvbuf

```lua
file:setvbuf(mode [, size])
```

Sets the buffering mode for an output file.

**Modes:**

- `"no"` - no buffering
- `"full"` - full buffering
- `"line"` - line buffering

#### file:write

```lua
file:write(...)
```

Writes strings or numbers to the file (same as `io.write`).

______________________________________________________________________

## 5.8 Operating System Facilities

The OS library provides operating system facilities.

### os.clock

```lua
os.clock()
```

Returns an approximation of the amount of CPU time used by the program, in seconds.

### os.date

```lua
os.date([format [, time]])
```

Returns a string or a table containing date and time, formatted according to the given string `format`.

If `time` is given, this is the time to be formatted (see `os.time`). Otherwise uses current time.

If `format` starts with `!`, date is formatted in UTC.

**Format specifiers** (same as C's `strftime`):

- `%a` - abbreviated weekday name
- `%A` - full weekday name
- `%b` - abbreviated month name
- `%B` - full month name
- `%c` - date and time representation
- `%d` - day of month (01-31)
- `%H` - hour (00-23)
- `%I` - hour (01-12)
- `%j` - day of year (001-366)
- `%m` - month (01-12)
- `%M` - minute (00-59)
- `%p` - AM/PM
- `%S` - second (00-61)
- `%U` - week of year (Sunday first day)
- `%w` - weekday (0-6, Sunday is 0)
- `%W` - week of year (Monday first day)
- `%x` - date representation
- `%X` - time representation
- `%y` - two-digit year
- `%Y` - four-digit year
- `%Z` - timezone name
- `%%` - literal %

If format is `"*t"`, returns a table with fields: `year`, `month`, `day`, `hour`, `min`, `sec`, `wday`, `yday`, `isdst`.

### os.difftime

```lua
os.difftime(t2, t1)
```

Returns the number of seconds from time `t1` to time `t2`.

### os.execute

```lua
os.execute([command])
```

Passes `command` to be executed by an operating system shell.

Returns a status code (system-dependent). If `command` is omitted, returns non-zero if a shell is available.

### os.exit

```lua
os.exit([code])
```

Calls the C function `exit` to terminate the host program.

`code` is the status code (defaults to success status).

### os.getenv

```lua
os.getenv(varname)
```

Returns the value of the environment variable `varname`, or nil if the variable is not defined.

### os.remove

```lua
os.remove(filename)
```

Deletes the file or directory with the given name.

Returns true on success; otherwise returns nil plus an error message.

### os.rename

```lua
os.rename(oldname, newname)
```

Renames file or directory named `oldname` to `newname`.

Returns true on success; otherwise returns nil plus an error message.

### os.setlocale

```lua
os.setlocale(locale [, category])
```

Sets the current locale of the program.

**Categories:**

- `"all"` (default)
- `"collate"`
- `"ctype"`
- `"monetary"`
- `"numeric"`
- `"time"`

Returns the name of the new locale, or nil if the request cannot be honored.

### os.time

```lua
os.time([table])
```

Returns the current time when called without arguments, or a time representing the date and time specified by the given table.

The table must have fields: `year`, `month`, `day`. Optional fields: `hour` (default 12), `min` (default 0), `sec` (default 0), `isdst` (daylight saving flag).

Returns a number (the meaning of which depends on the system; on POSIX, it's seconds since epoch).

**Example:**

```lua
t = os.time({year=2023, month=5, day=17, hour=10, min=30})
```

### os.tmpname

```lua
os.tmpname()
```

Returns a string with a file name that can be used for a temporary file. The file must be explicitly removed when no longer needed.

______________________________________________________________________

## 5.9 The Debug Library

The debug library provides functionality for debugging and introspection. It should be used with care as it can compromise program security and performance.

### debug.debug

```lua
debug.debug()
```

Enters an interactive mode with the user, running each string that the user enters. Simple commands and expressions are evaluated and their results printed.

Type `cont` to exit.

### debug.getfenv

```lua
debug.getfenv(o)
```

Returns the environment of object `o`.

### debug.gethook

```lua
debug.gethook([thread])
```

Returns the current hook settings as three values: the hook function, the hook mask, and the hook count.

### debug.getinfo

```lua
debug.getinfo([thread,] function [, what])
```

Returns a table with information about a function.

`function` can be a function directly or a number (stack level: 0 = current function, 1 = calling function, etc.).

`what` is a string selecting which information to return:

- `"n"` - `name` and `namewhat` fields
- `"S"` - `source`, `short_src`, `linedefined`, `lastlinedefined`, `what` fields
- `"l"` - `currentline` field
- `"u"` - `nups` field (number of upvalues)
- `"f"` - `func` field (the function itself)
- `"L"` - `activelines` field (table with valid lines)

Default for `what` is all information available.

### debug.getlocal

```lua
debug.getlocal([thread,] level, local)
```

Returns the name and value of the local variable with index `local` at the given stack `level`.

Returns nil if there is no local variable at that level/index.

### debug.getmetatable

```lua
debug.getmetatable(object)
```

Returns the metatable of the given object or nil if it does not have a metatable.

Unlike `getmetatable`, this ignores the `__metatable` field.

### debug.getregistry

```lua
debug.getregistry()
```

Returns the registry table (a special table used to store Lua state).

### debug.getupvalue

```lua
debug.getupvalue(func, up)
```

Returns the name and value of the upvalue with index `up` of function `func`.

Returns nil if there is no upvalue with that index.

### debug.setfenv

```lua
debug.setfenv(object, table)
```

Sets the environment of the given object to the given table.

Returns the object.

### debug.sethook

```lua
debug.sethook([thread,] hook, mask [, count])
```

Sets the given function as a hook. The hook is called on specific events determined by `mask`:

- `"c"` - call hook (called when Lua calls a function)
- `"r"` - return hook (called when Lua returns from a function)
- `"l"` - line hook (called when Lua enters a new line of code)

`count` is optional: when different from zero, the hook is called after every `count` instructions.

### debug.setlocal

```lua
debug.setlocal([thread,] level, local, value)
```

Assigns `value` to the local variable with index `local` at stack level `level`.

Returns the name of the local variable or nil if there is no local variable at that level/index.

### debug.setmetatable

```lua
debug.setmetatable(object, table)
```

Sets the metatable of the given object to the given table (can be nil).

Unlike `setmetatable`, this ignores the `__metatable` field protection.

### debug.setupvalue

```lua
debug.setupvalue(func, up, value)
```

Assigns `value` to the upvalue with index `up` of function `func`.

Returns the name of the upvalue or nil if there is no upvalue with that index.

### debug.traceback

```lua
debug.traceback([thread,] [message [, level]])
```

Returns a string with a traceback of the call stack.

Optional `message` is appended at the beginning of the traceback.

Optional `level` specifies at which level to start the traceback (default is 1, the function calling `traceback`).

______________________________________________________________________

## Summary

This document provides comprehensive coverage of all Lua 5.1 standard library functions across nine major modules:

1. **Basic Functions** - Core global functions for type manipulation, environment control, error handling, and metatable operations
1. **Coroutine Manipulation** - Cooperative multithreading primitives
1. **Modules** - Package loading and management system
1. **String Manipulation** - String operations and powerful pattern matching
1. **Table Manipulation** - Array-like operations on tables
1. **Mathematical Functions** - Complete math library with trigonometric, logarithmic, and utility functions
1. **I/O Facilities** - File and stream input/output operations
1. **OS Facilities** - Operating system interface for time, environment, and file operations
1. **Debug Library** - Introspection and debugging tools

For language-level features (syntax, semantics, metatables, etc.), refer to the main Lua 5.1 reference document.

______________________________________________________________________

**References:**

- Official Lua 5.1 Reference Manual: https://www.lua.org/manual/5.1/manual.html
- Programming in Lua (first edition): https://www.lua.org/pil/
