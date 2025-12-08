# Lua 5.1 Complete Reference Documentation

**Source:** Official Lua 5.1 Reference Manual
**Authors:** R. Ierusalimschy, L. H. de Figueiredo, W. Celes
**Published:** August 2006
**ISBN:** 85-903798-3-3

______________________________________________________________________

## Table of Contents

1. [Introduction](#1-introduction)
1. [The Language](#2-the-language)
   - [2.1 Lexical Conventions](#21-lexical-conventions)
   - [2.2 Values and Types](#22-values-and-types)
   - [2.3 Variables](#23-variables)
   - [2.4 Statements](#24-statements)
   - [2.5 Expressions](#25-expressions)
   - [2.6 Visibility Rules](#26-visibility-rules)
   - [2.7 Error Handling](#27-error-handling)
   - [2.8 Metatables](#28-metatables)
   - [2.9 Environments](#29-environments)
   - [2.10 Garbage Collection](#210-garbage-collection)
   - [2.11 Coroutines](#211-coroutines)
1. [Standard Libraries](#3-standard-libraries) *(Partial - See note below)*

______________________________________________________________________

## 1. Introduction

Lua is an extension programming language designed to support general procedural programming with data description facilities. It also offers good support for object-oriented programming, functional programming, and data-driven programming. Lua is intended to be used as a powerful, lightweight scripting language for any program that needs one.

Lua is implemented as a library written in clean C (the common subset of ANSI C and C++), and is highly portable. The implementation is register-based, distinguishing it from many stack-based virtual machines.

______________________________________________________________________

## 2. The Language

Lua is dynamically typed: values carry type information, not variables. This section describes the lexical elements, semantics, and syntax of Lua 5.1.

### 2.1 Lexical Conventions

#### Identifiers and Keywords

Lua identifiers consist of **any string of letters, digits, and underscores, not beginning with a digit**. The language is case-sensitive, meaning `and` is reserved but `And` and `AND` are distinct valid identifiers.

By convention, identifiers starting with an underscore followed by uppercase letters (such as `_VERSION`) are reserved for Lua's internal global variables.

#### Reserved Keywords

The following keywords are reserved and cannot be used as identifiers:

```
and       break     do        else      elseif    end
false     for       function  if        in        local
nil       not       or        repeat    return    then
true      until     while
```

#### Operators and Delimiters

Lua recognizes the following operator and delimiter tokens:

```
+     -     *     /     %     ^     #
==    ~=    <=    >=    <     >     =
(     )     {     }     [     ]     ;
:     ,     .     ..    ...
```

#### String Literals

Strings use matching single or double quotes. They support C-like escape sequences:

- `\a` - bell (ASCII 7)
- `\b` - backspace (ASCII 8)
- `\f` - form feed (ASCII 12)
- `\n` - newline (ASCII 10)
- `\r` - carriage return (ASCII 13)
- `\t` - horizontal tab (ASCII 9)
- `\v` - vertical tab (ASCII 11)
- `\\` - backslash
- `\"` - double quote
- `\'` - single quote
- `\ddd` - character with decimal code `ddd` (up to 3 digits)

**Long Bracket Strings** use `[[...]]` syntax to span multiple lines without interpreting escape sequences. They can nest using equal signs for disambiguation: `[=[...]=]`, `[==[...]==]`, etc.

#### Numerical Constants

Valid numeric formats include:

- Decimal integers: `3`, `42`
- Decimal floating-point: `3.0`, `3.1416`, `314.16e-2`, `0.31416E1`
- Hexadecimal integers: `0xff`, `0x56` (must use `0x` or `0X` prefix)

#### Comments

**Short comments** begin with `--` and extend to the end of the line.

**Long comments** use `--[[...]]` syntax and can span multiple lines. Like long strings, they support nesting with equal signs: `--[=[...]=]`.

______________________________________________________________________

### 2.2 Values and Types

Lua employs **dynamic typing**: values—not variables—carry type information. All values are first-class entities, capable of being stored in variables, passed as arguments, and returned as results.

#### The Eight Basic Types

**nil**
The type of the value `nil`, whose main property is to be different from any other value. It usually represents the absence of a useful value.

**boolean**
Contains only the values `false` and `true`. In conditional contexts, both `nil` and `false` evaluate to false; **all other values are considered true** (including `0` and the empty string).

**number**
Represents real double-precision floating-point values. The implementation permits alternative representations like single-precision floats or long integers through configuration adjustments, though double-precision is standard.

**string**
Represents immutable arrays of characters. Lua is 8-bit clean: strings can contain any 8-bit character, including embedded zeros (`\0`). Strings are immutable; operations that modify strings create new strings.

**function**
Lua functions can be written in either Lua or C. Both are fully interchangeable through the API and can be stored in variables, passed as arguments, and returned as results.

**userdata**
Permits storage of arbitrary C data within Lua. This type corresponds to a block of raw memory and has no pre-defined operations in Lua, except assignment and identity testing. Metatables enable custom operations on userdata.

**thread**
Represents independent threads of execution and is used to implement coroutines. Lua threads differ fundamentally from operating system threads—they implement cooperative multithreading.

**table**
Implements associative arrays that can be indexed with any value (except `nil`). Tables can hold values of any type. They are the sole data structuring mechanism in Lua, used to represent arrays, sets, records, graphs, trees, and more. Tables use reference semantics—variables hold references to tables, not the tables themselves.

#### Type Coercion

Lua provides automatic conversions between strings and numbers at runtime:

**String to Number**: Arithmetic operations automatically convert string operands to numbers if the string represents a valid numerical constant.

**Number to String**: When a number appears in a context requiring a string (like concatenation), it converts automatically to string format.

For explicit conversions, use `tonumber()` and `tostring()` functions.

______________________________________________________________________

### 2.3 Variables

Lua supports three categories of variables:

1. **Global variables**
1. **Local variables**
1. **Table fields**

#### Global Variables

Any variable not explicitly declared as `local` is global. Global variables are stored as fields in **environment tables**. The expression `x` in a function effectively accesses `_env.x`, where `_env` represents that function's environment table.

Before assignment, global variables have the value `nil`.

#### Local Variables

Variables declared with the `local` keyword exhibit **lexical scoping**. Local variables are accessible within their declaring block and in nested functions defined within that scope. They exist only from the point of declaration until the end of their enclosing block.

```lua
local x = 10
if x > 5 then
    local y = x * 2  -- y is local to this if block
    print(y)
end
-- y is not accessible here
```

#### Table Fields

Tables are indexed using square brackets: `t[i]`. Lua provides syntactic sugar for string keys: `t.name` is equivalent to `t["name"]`.

Table fields can store values of any type, and missing fields have the value `nil`.

______________________________________________________________________

### 2.4 Statements

Lua supports a conventional set of statements similar to Pascal or C.

#### Syntax Overview

A **chunk** is a sequence of statements executed sequentially:

```
chunk ::= {stat [`;´]}
```

Semicolons between statements are optional. **There are no empty statements**, so `;;` is not legal Lua.

#### Blocks

A **block** is a list of statements with the same syntax as chunks. Explicit blocks use `do...end` to control variable scope:

```lua
do
    local temp = x
    x = y
    y = temp
end
-- temp is not accessible here
```

#### Assignment

Lua supports multiple simultaneous assignments with automatic value list adjustment:

```lua
x, y = y, x  -- exchanges values of x and y
a, b, c = 1, 2  -- a=1, b=2, c=nil
a, b = 1, 2, 3  -- a=1, b=2 (3 is discarded)
```

**Assignment semantics:**

1. All expressions on the right side are evaluated
1. Values are adjusted to match the number of variables on the left
1. Excess values are discarded; missing values default to `nil`
1. Assignments are performed left-to-right

Function calls that return multiple values expand into the value list (unless parenthesized, which limits them to one return value).

#### Control Structures

**Conditional: if...then...else...end**

```lua
if condition then
    -- statements
elseif another_condition then
    -- statements
else
    -- statements
end
```

**Conditionals in Lua:** Both `false` and `nil` are considered false. All other values (including `0` and empty string `""`) are considered true.

**While Loop**

```lua
while condition do
    -- statements
end
```

The condition is evaluated before each iteration.

**Repeat-Until Loop**

```lua
repeat
    -- statements
until condition
```

The block executes at least once. **The inner block does not end at the `until` keyword**—the condition can reference local variables declared inside the loop.

**Numeric For Loop**

```lua
for var = start, limit, step do
    -- statements
end
```

- `start`, `limit`, and `step` are evaluated once before the loop begins
- `step` is optional (defaults to 1)
- The loop variable `var` is local to the loop and should not be modified
- The loop continues while `var <= limit` (for positive step) or `var >= limit` (for negative step)

**Generic For Loop (Iterator)**

```lua
for var1, var2, ..., varN in iterator_expression do
    -- statements
end
```

The generic `for` calls the iterator function repeatedly, receiving a new set of values on each iteration. The loop terminates when the first returned value is `nil`.

Common iterators: `pairs(t)` (all key-value pairs), `ipairs(t)` (integer keys in sequence).

#### Break and Return

**break** terminates the innermost enclosing loop (while, repeat, or for). It must be the last statement in a block (use explicit `do...end` if needed).

**return** exits a function, optionally returning values:

```lua
return expression_list
```

Return can only appear as the last statement of a block. To return in the middle of a block, use `do return end`.

#### Function Calls as Statements

Function calls can execute as statements, discarding return values:

```lua
print("Hello, World!")
my_function(arg1, arg2)
```

#### Local Declarations

```lua
local name1, name2, ..., nameN = expression_list
```

Local variables are visible from the point of declaration until the end of the block. Initial values default to `nil` if not provided.

______________________________________________________________________

### 2.5 Expressions

Expressions compute values in Lua. Expression types include:

- Literals (`nil`, `false`, `true`, numbers, strings)
- Variables
- Table constructors
- Function definitions
- Function calls
- Vararg expression (`...`)
- Unary and binary operations
- Parenthesized expressions

#### 2.5.1 Arithmetic Operators

Lua provides six binary arithmetic operators:

- `+` addition
- `-` subtraction
- `*` multiplication
- `/` division
- `%` modulo
- `^` exponentiation

And one unary operator:

- `-` negation

When both operands are numbers (or strings convertible to numbers), operations proceed with their mathematical meanings. Exponentiation and float division always produce floating-point results.

**Modulo definition:** `a % b == a - math.floor(a/b)*b`

This represents the remainder of division rounding quotient toward negative infinity. This differs from some languages that round toward zero.

#### 2.5.2 Relational Operators

Lua provides six relational operators:

```
==    ~=    <     >     <=    >=
```

These always produce boolean results (`true` or `false`).

**Equality (`==` and `~=`):**

- Different types are always unequal
- `nil` equals only `nil`
- Numbers and strings compare by value
- Tables, userdata, functions, and threads compare by reference (identity)
- Metatables can customize equality via the `__eq` metamethod

**Order operators (`<`, `>`, `<=`, `>=`):**

- Two numbers compare mathematically
- Two strings compare lexicographically (locale-aware)
- Different types cannot be compared (raises error)
- Metatables can customize via `__lt` (less than) and `__le` (less or equal) metamethods

#### 2.5.3 Logical Operators

Lua provides three logical operators:

- `and`
- `or`
- `not`

**Truth values:** Like control structures, `false` and `nil` are false; all other values are true.

**not:** Always returns `true` or `false`.

```lua
not nil        --> true
not false      --> true
not 0          --> false
not ""         --> false
```

**and:** Returns its first argument if false or nil; otherwise returns its second argument.

```lua
nil and 10     --> nil
false and 10   --> false
5 and 10       --> 10
```

**or:** Returns its first argument unless it's false or nil; otherwise returns its second argument.

```lua
nil or 10      --> 10
false or 10    --> 10
5 or 10        --> 5
```

Both `and` and `or` use **short-circuit evaluation**: the second operand is evaluated only when necessary.

#### 2.5.4 Concatenation

The concatenation operator is `..` (two dots). It joins strings:

```lua
"Hello " .. "World"  --> "Hello World"
```

When both operands are strings or numbers, they convert to strings and concatenate. Otherwise, the `__concat` metamethod is invoked.

#### 2.5.5 Length Operator

The unary length operator is `#`.

**For strings:** Returns the byte count.

```lua
#"abc"        --> 3
#"a\0bc"      --> 4
```

**For tables:** Returns a "border" index—an integer `n` such that `t[n]` is not `nil` but `t[n+1]` is `nil`. If `t[1]` is `nil`, returns 0.

For sequences (tables with contiguous integer keys starting at 1), `#t` returns the length. For tables with holes, the result is undefined but will be one of the borders.

```lua
#{10, 20, 30}             --> 3
#{10, 20, nil, 40}        --> 2 or 4 (undefined)
```

#### 2.5.6 Operator Precedence

Operator precedence from lowest to highest:

```
or
and
<   >   <=  >=  ~=  ==
..
+   -
*   /   %
not  #  - (unary)
^
```

**Associativity:**

- Concatenation (`..`) is right-associative
- Exponentiation (`^`) is right-associative
- All other binary operators are left-associative

Use parentheses to override default precedence.

#### 2.5.7 Table Constructors

Table constructors create and initialize tables:

```lua
{expression1, expression2, ...}  -- array part
{key1 = value1, key2 = value2}   -- hash part
{[expr1] = expr2, name = expr3}  -- mixed
```

**Field syntax:**

1. `[exp1] = exp2` - explicit key-value pair
1. `name = exp` - equivalent to `["name"] = exp`
1. `exp` - equivalent to `[i] = exp` where `i` starts at 1 and increments

**Example:**

```lua
t = {x = 10, y = 20, "a", "b", "c"}
-- Equivalent to:
-- t = {}
-- t.x = 10
-- t.y = 20
-- t[1] = "a"
-- t[2] = "b"
-- t[3] = "c"
```

If the last field is a function call or vararg expression (`...`), all its return values enter the list consecutively. To force a single value, enclose in parentheses.

#### 2.5.8 Function Calls

Function call syntax:

```lua
functionname(arguments)
object:methodname(arguments)  -- method call syntax sugar
```

**Method calls:** `v:name(args)` is syntactic sugar for `v.name(v, args)`, passing the object as the first argument (conventionally called `self`).

**Special call forms:**

- `f{fields}` - sugar for `f({fields})` (table constructor as single argument)
- `f'string'` or `f"string"` - sugar for `f('string')` (string literal as single argument)

**Important restriction:** No line break may precede the opening parenthesis, bracket, or quote in a function call. This prevents ambiguity.

```lua
-- This is valid:
f(x)

-- This is parsed as two statements, not a call:
f
(x)
```

**Tail Calls**

A tail call occurs when a `return` statement contains only a single function call:

```lua
function f(x)
    return g(x)  -- tail call
end
```

Tail calls reuse the calling function's stack frame, allowing unlimited tail recursion without stack overflow.

These are **not** tail calls:

```lua
return g(x) + 1     -- arithmetic after call
return g(x), h(x)   -- multiple returns
return (g(x))       -- parentheses force adjustment
```

#### 2.5.9 Function Definitions

Functions are first-class values defined with this syntax:

```lua
function funcbody
funcbody ::= (parlist) block end
parlist ::= namelist [ , ... ] | ...
```

**Basic definition:**

```lua
function add(a, b)
    return a + b
end
```

**Syntactic sugar:**

These are equivalent:

```lua
function f() body end
f = function() body end
```

```lua
function t.a.b.c.f() body end
t.a.b.c.f = function() body end
```

```lua
local function f() body end
local f; f = function() body end
```

(Note: local function definitions bind the name before evaluating the function, allowing recursion.)

**Method definitions:**

```lua
function t:method(params) body end
-- Equivalent to:
t.method = function(self, params) body end
```

**Variadic Functions**

Functions ending with `...` in their parameter list accept a variable number of arguments:

```lua
function sum(...)
    local s = 0
    for _, v in ipairs{...} do
        s = s + v
    end
    return s
end
```

Inside a variadic function, the expression `...` evaluates to all extra arguments.

**Closures**

Functions defined inside other functions capture external local variables (upvalues):

```lua
function makeCounter()
    local count = 0
    return function()
        count = count + 1
        return count
    end
end

counter = makeCounter()
print(counter())  --> 1
print(counter())  --> 2
```

Each function instantiation has independent upvalues.

______________________________________________________________________

### 2.6 Visibility Rules

Lua uses **lexical scoping**. Local variables have scope beginning after their declaration and extending to the end of their enclosing block.

**Example:**

```lua
x = 10              -- global x
do
    local x = x     -- new local x, initialized with global x
    print(x)        --> 10
    x = x + 1
    do
        local x = x + 1  -- another local x
        print(x)         --> 12
    end
    print(x)        --> 11
end
print(x)            --> 10 (global)
```

**Upvalues:** Inner functions access outer local variables. These captured variables are called upvalues:

```lua
function outer()
    local x = 0
    function inner()
        x = x + 1  -- x is an upvalue
        return x
    end
    return inner
end
```

Each execution of the outer function creates new local variables. Multiple closures from the same outer execution share the same upvalue instances.

______________________________________________________________________

### 2.7 Error Handling

Lua code can explicitly raise errors using `error(message, level)`. Errors propagate up the call stack until caught by `pcall` or `xpcall`, or until they reach the host program (typically terminating the script).

**Protected Calls:**

`pcall(f, arg1, arg2, ...)` calls function `f` in protected mode. It returns:

- `true, result1, result2, ...` on success
- `false, error_message` on error

Example:

```lua
local success, result = pcall(function()
    return 10 / 0
end)
if success then
    print("Result:", result)
else
    print("Error:", result)
end
```

`xpcall(f, msgh)` calls `f` with a custom message handler `msgh` that processes errors before propagation.

______________________________________________________________________

### 2.8 Metatables

Metatables enable customizing the behavior of values through metamethods. Each value can have an associated metatable—a regular Lua table defining special behaviors.

**Setting Metatables:**

```lua
t = {}
mt = {__add = function(a, b) return {sum = true} end}
setmetatable(t, mt)
```

Tables and userdata have individual metatables. Values of other types share one metatable per type (modifiable only from C).

#### Metamethods Reference

Metatables define behavior through special keys:

**Arithmetic Metamethods:**

- `__add(a, b)` - addition (`a + b`)
- `__sub(a, b)` - subtraction (`a - b`)
- `__mul(a, b)` - multiplication (`a * b`)
- `__div(a, b)` - division (`a / b`)
- `__mod(a, b)` - modulo (`a % b`)
- `__pow(a, b)` - exponentiation (`a ^ b`)
- `__unm(a)` - negation (`-a`)

For binary operations, Lua tries the left operand's metatable first, then the right operand's. If neither has the appropriate metamethod, an error is raised.

**Relational Metamethods:**

- `__eq(a, b)` - equality (`a == b`)
  - Only invoked when both operands have the same type and the same metamethod
- `__lt(a, b)` - less than (`a < b`)
  - `a > b` translates to `b < a`
- `__le(a, b)` - less than or equal (`a <= b`)
  - `a >= b` translates to `b <= a`
  - If `__le` is not defined, Lua tries `not (b < a)` using `__lt`

**String/Length Metamethods:**

- `__concat(a, b)` - concatenation (`a .. b`)
- `__len(a)` - length operator (`#a`)

**Indexing Metamethods:**

- `__index(table, key)` - table access (`table[key]`)

  - Invoked when `key` is not present in `table`
  - Can be a function or a table (used as fallback for lookups)

- `__newindex(table, key, value)` - table assignment (`table[key] = value`)

  - Invoked when `key` is not present in `table`
  - Can be a function or a table (fallback for assignments)

Example:

```lua
t = {}
mt = {
    __index = function(table, key)
        return "Key " .. key .. " not found"
    end
}
setmetatable(t, mt)
print(t.foo)  --> "Key foo not found"
```

**Call Metamethod:**

- `__call(func, ...)` - function call (`func(...)`)
  - Allows non-function values to be called as functions

**Other Metamethods:**

- `__tostring(v)` - string conversion (used by `tostring()`)

  - Must return a string

- `__metatable` - protects metatable

  - When set, `getmetatable()` returns this value instead
  - `setmetatable()` raises an error

- `__mode` - controls weak references (see Garbage Collection)

  - String containing `'k'` (weak keys), `'v'` (weak values), or both

- `__gc(udata)` - finalizer for userdata

  - Called before garbage collection (see section 2.10)

#### Retrieving Metamethods

To retrieve a metamethod, Lua uses:

```lua
rawget(getmetatable(obj) or {}, "__eventname")
```

This avoids invoking other metamethods during lookup.

______________________________________________________________________

### 2.9 Environments

Every function has an associated **environment table** used for resolving global variable accesses. When a function references a global variable `x`, it effectively accesses `_env.x` where `_env` is its environment.

**Getting and Setting Environments:**

- `getfenv(f)` - returns the environment of function `f` (or current function if `f` is omitted or 0)
- `setfenv(f, table)` - sets the environment of function `f` to `table`

Functions defined in Lua inherit the environment of their creating function. This allows creating sandboxed execution contexts:

```lua
-- Create a sandboxed environment
local sandbox = {print = print}
local f = function()
    print("Hello from sandbox")
    -- Can only access what's in sandbox
end
setfenv(f, sandbox)
f()  --> "Hello from sandbox"
```

The global variable `_G` initially holds the global environment itself: `_G._G == _G`.

______________________________________________________________________

### 2.10 Garbage Collection

Lua performs automatic memory management using an **incremental mark-and-sweep garbage collector**. All objects (tables, functions, threads, strings, userdata) are subject to automatic collection when no longer referenced.

#### Garbage Collector Parameters

Two parameters control collector behavior:

1. **Garbage-collector pause** (percentage)

   - Controls how long the collector waits before starting a new cycle
   - Value of 200 means waiting until memory doubles
   - Smaller values make collection more aggressive

1. **Step multiplier** (percentage)

   - Controls collector speed relative to memory allocation
   - Value of 200 means the collector runs at twice the speed of allocation
   - Larger values make collection more aggressive

**Control function:** `collectgarbage(opt [, arg])`

Options include:

- `"collect"` - performs a full garbage-collection cycle
- `"stop"` - stops the garbage collector
- `"restart"` - restarts the garbage collector
- `"count"` - returns memory usage in kilobytes
- `"step"` - performs a garbage-collection step
- `"setpause"` - sets the `pause` parameter
- `"setstepmul"` - sets the `step multiplier` parameter

#### 2.10.1 Garbage-Collection Metamethods (Finalizers)

Userdata objects can have finalizers through the `__gc` metamethod:

```lua
udata = newproxy(true)
getmetatable(udata).__gc = function(obj)
    print("Object being collected")
end
```

When a userdata with a `__gc` metamethod becomes unreachable, it's marked for finalization. After the collection cycle completes, finalizers run in **reverse order of creation** (last created, first finalized).

The userdata is freed in the next collection cycle after finalization completes.

#### 2.10.2 Weak Tables

**Weak tables** allow garbage collection of their keys or values even when referenced by the table. The `__mode` metatable field controls weakness:

- `__mode = "k"` - weak keys (keys can be collected)
- `__mode = "v"` - weak values (values can be collected)
- `__mode = "kv"` - both weak

When a key or value in a weak table is collected, the entire entry is removed from the table.

```lua
weak = {}
setmetatable(weak, {__mode = "v"})
key = {}
weak[1] = key
key = nil
collectgarbage()
-- weak[1] is now nil
```

**Rules:**

- Only tables and userdata can be collected from weak tables
- Values like numbers and booleans are never collected (they're values, not objects)
- Strings may or may not be collected depending on implementation

______________________________________________________________________

### 2.11 Coroutines

Lua supports **coroutines** for collaborative multithreading. A coroutine represents an independent thread of execution with its own stack and local variables. Unlike operating system threads, coroutines execute cooperatively: a coroutine only suspends by explicitly yielding.

#### Coroutine States

A coroutine can be in one of four states:

- **suspended** - created but not started, or yielded
- **running** - currently executing
- **normal** - has resumed another coroutine
- **dead** - finished execution or terminated with error

#### Creating Coroutines

`coroutine.create(f)` - creates a new coroutine with function `f` as its body. Returns a thread object (type `"thread"`). The coroutine starts in suspended state.

```lua
co = coroutine.create(function()
    print("Hello from coroutine")
end)
```

#### Running Coroutines

`coroutine.resume(co [, val1, ...])` - starts or resumes coroutine `co`. Arguments are passed to the coroutine:

- On first resume, they become arguments to the coroutine's main function
- On subsequent resumes, they become return values of the `yield` call

Returns:

- `true, results...` - if coroutine yields or completes successfully
- `false, error_message` - if an error occurs

```lua
co = coroutine.create(function(a, b)
    print("Received:", a, b)
    return a + b
end)
success, result = coroutine.resume(co, 10, 20)
-- Prints: Received: 10 20
print(success, result)  --> true 30
```

#### Yielding

`coroutine.yield(...)` - suspends the calling coroutine. Arguments become extra return values of `resume`.

Yielding can occur inside nested function calls:

```lua
function foo()
    coroutine.yield("yielding from foo")
end

co = coroutine.create(function()
    foo()
    print("Resumed")
end)

print(coroutine.resume(co))  --> true  yielding from foo
print(coroutine.resume(co))  --> true (and prints "Resumed")
```

#### Coroutine Utilities

`coroutine.status(co)` - returns the status string: `"suspended"`, `"running"`, `"normal"`, or `"dead"`

`coroutine.running()` - returns the currently running coroutine (or `nil` if called from main thread)

`coroutine.wrap(f)` - creates a coroutine like `create`, but returns a function that resumes it:

```lua
co = coroutine.wrap(function(x)
    coroutine.yield(x * 2)
    return x * 3
end)

print(co(10))  --> 20
print(co())    --> 30
```

Unlike `resume`, `wrap` does not catch errors—they propagate to the caller.

#### Coroutine Example: Producer-Consumer

```lua
function producer()
    while true do
        local x = io.read()
        coroutine.yield(x)
    end
end

function consumer(prod)
    while true do
        local x = coroutine.resume(prod)
        print(x)
    end
end

consumer(coroutine.create(producer))
```

______________________________________________________________________

## 3. Standard Libraries

**NOTE:** The official Lua 5.1 manual's Section 5 (Standard Libraries) content was not fully accessible through web scraping. The following sections should contain detailed documentation for:

### 3.1 Basic Functions (Section 5.1)

Functions that should be documented: `assert`, `collectgarbage`, `dofile`, `error`, `_G`, `getfenv`, `getmetatable`, `ipairs`, `load`, `loadfile`, `loadstring`, `next`, `pairs`, `pcall`, `print`, `rawequal`, `rawget`, `rawset`, `select`, `setfenv`, `setmetatable`, `tonumber`, `tostring`, `type`, `unpack`, `_VERSION`, `xpcall`

### 3.2 Coroutine Manipulation (Section 5.2)

Functions: `coroutine.create`, `coroutine.resume`, `coroutine.running`, `coroutine.status`, `coroutine.wrap`, `coroutine.yield`

### 3.3 Modules (Section 5.3)

Functions: `module`, `require`, `package.cpath`, `package.loaded`, `package.loadlib`, `package.path`, `package.preload`, `package.seeall`

### 3.4 String Manipulation (Section 5.4)

Functions: `string.byte`, `string.char`, `string.dump`, `string.find`, `string.format`, `string.gmatch`, `string.gsub`, `string.len`, `string.lower`, `string.match`, `string.rep`, `string.reverse`, `string.sub`, `string.upper`

**Pattern Matching:** Character classes, magic characters, captures, pattern items, frontier patterns

### 3.5 Table Manipulation (Section 5.5)

Functions: `table.concat`, `table.insert`, `table.maxn`, `table.remove`, `table.sort`

### 3.6 Mathematical Functions (Section 5.6)

Functions: `math.abs`, `math.acos`, `math.asin`, `math.atan`, `math.atan2`, `math.ceil`, `math.cos`, `math.cosh`, `math.deg`, `math.exp`, `math.floor`, `math.fmod`, `math.frexp`, `math.huge`, `math.ldexp`, `math.log`, `math.log10`, `math.max`, `math.min`, `math.modf`, `math.pi`, `math.pow`, `math.rad`, `math.random`, `math.randomseed`, `math.sin`, `math.sinh`, `math.sqrt`, `math.tan`, `math.tanh`

### 3.7 Input and Output Facilities (Section 5.7)

Functions: `io.close`, `io.flush`, `io.input`, `io.lines`, `io.open`, `io.output`, `io.popen`, `io.read`, `io.stderr`, `io.stdin`, `io.stdout`, `io.tmpfile`, `io.type`, `io.write`, file methods

### 3.8 Operating System Facilities (Section 5.8)

Functions: `os.clock`, `os.date`, `os.difftime`, `os.execute`, `os.exit`, `os.getenv`, `os.remove`, `os.rename`, `os.setlocale`, `os.time`, `os.tmpname`

### 3.9 The Debug Library (Section 5.9)

Functions: `debug.debug`, `debug.getfenv`, `debug.gethook`, `debug.getinfo`, `debug.getlocal`, `debug.getmetatable`, `debug.getregistry`, `debug.getupvalue`, `debug.setfenv`, `debug.sethook`, `debug.setlocal`, `debug.setmetatable`, `debug.setupvalue`, `debug.traceback`

______________________________________________________________________

## Quick Reference

### All Lua 5.1 Keywords

```
and       break     do        else      elseif    end
false     for       function  if        in        local
nil       not       or        repeat    return    then
true      until     while
```

### Operator Precedence (Lowest to Highest)

```
1.  or
2.  and
3.  <   >   <=  >=  ~=  ==
4.  ..
5.  +   -
6.  *   /   %
7.  not  #  - (unary)
8.  ^
```

### All Metamethods

```
__add       __sub       __mul       __div       __mod       __pow
__unm       __concat    __len       __eq        __lt        __le
__index     __newindex  __call      __tostring  __metatable __mode
__gc
```

### Common Patterns

**Iterate array:**

```lua
for i, v in ipairs(array) do
    print(i, v)
end
```

**Iterate table:**

```lua
for k, v in pairs(table) do
    print(k, v)
end
```

**Protected call:**

```lua
local success, result = pcall(function()
    -- code that might error
end)
```

**Class-like tables:**

```lua
MyClass = {}
MyClass.__index = MyClass

function MyClass:new()
    local instance = setmetatable({}, self)
    return instance
end

function MyClass:method()
    -- self is automatically passed
end
```

______________________________________________________________________

## Additional Resources

For complete function signatures and descriptions of the standard library functions, please consult:

- **Official Lua 5.1 Reference Manual:** https://www.lua.org/manual/5.1/manual.html
- **Programming in Lua (first edition):** Free online at https://www.lua.org/pil/
- **Lua-users wiki:** http://lua-users.org/wiki/

______________________________________________________________________

## Notes on This Document

This reference document was compiled from the official Lua 5.1 Reference Manual using web scraping techniques. Sections 1-2 (Introduction and The Language) are complete with full details on lexical conventions, types, variables, statements, expressions, metatables, environments, garbage collection, and coroutines.

Section 3 (Standard Libraries) header information is included but detailed function signatures and descriptions could not be extracted via automated web fetching. Please refer to the official manual at lua.org for complete standard library documentation.

For NovaSharp implementation purposes, this document provides comprehensive coverage of Lua 5.1 language semantics, which is essential for interpreter correctness.
