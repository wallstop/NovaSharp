# Lua 5.5 Reference Documentation - Work-in-Progress Version

> **Note**: Lua 5.5 is currently in release candidate phase. This documentation reflects the state as of December 2025.

## Overview

Lua 5.5 is the upcoming version of Lua currently in release candidate phase. As of December 2025, **Lua 5.5.0 (rc2)** was released on December 1, 2025, following rc1 on November 15, 2025, and a beta release on June 28, 2025. The current stable release remains Lua 5.4.8 (released June 4, 2025).

## Release Status

- **Latest Work Version**: Lua 5.5.0-rc2 (December 1, 2025)
- **Status**: Final release-candidate cycle
- **Available Downloads**:
  - lua-5.5.0-rc2.tar.gz (397,952 bytes)
  - lua-5.5.0-rc1.tar.gz (396,959 bytes)
  - lua-5.5.0-beta.tar.gz (393,374 bytes)
  - Test suite: lua-5.5.0-tests.tar.gz (148,929 bytes)

______________________________________________________________________

## Major New Features

### 1. Global Variable Declarations

**NEW**: Lua 5.5 introduces the `global` keyword for explicit declaration of global variables.

- `global` is now a **reserved keyword** appearing alongside: and, break, do, else, elseif, end, false, for, function, goto, if, in, local, nil, not, or, repeat, return, then, true, until, while
- All chunks start with an implicit declaration `global *`, which declares all free names as global variables
- This allows stricter scoping rules by requiring explicit global declarations within specific blocks

**Syntax**:

```lua
global x          -- declares x as global
global x, y, z    -- declares multiple globals
global *          -- declares all free names as global (implicit at chunk start)
```

**Backward Compatibility**:

- The `global` keyword can act as both a keyword and a variable name "for compatibility only"
- Old code may use 'global' as a name (e.g., a LuaRocks package defines a function 'global' that declares globals)
- You can undefine `LUA_COMPAT_GLOBAL` in luaconf.h to make 'global' a real reserved word

### 2. Read-Only For-Loop Variables

**BREAKING CHANGE**: For-loop variables are now **read-only (const)** by default.

- **Numeric for loops**: The control variable is a new read-only variable local to the loop body
- **Generic for loops**: Loop variables are local to the loop body, with the first being the read-only control variable

**Rationale**: This change was made for performance reasons. The old code does an implicit "local x = x" for all loops in case you modify 'x'. With the new semantics, the code only does it when you ask for it.

**Migration**: For code that relied on mutable loop variables, add `local k = k` at the top of the loop body to create a mutable shadow variable:

```lua
-- Old code (no longer works in 5.5):
for i = 1, 10 do
    i = i + 1  -- ERROR: attempt to assign to const variable
end

-- New code (5.5 compatible):
for i = 1, 10 do
    local i = i  -- Create mutable shadow
    i = i + 1    -- OK
end
```

### 3. Table.create Function

**NEW**: A new `table.create` function has been added to the standard table library.

```lua
table.create(narr, nrec)
```

This function allows pre-allocation of table capacity, useful for performance optimization when the size is known in advance.

- `narr`: Pre-allocate space for this many array elements
- `nrec`: Pre-allocate space for this many hash elements

### 4. Enhanced utf8.offset Function

**IMPROVED**: `utf8.offset` now returns **both the starting and final position** of the character.

```lua
local start_pos, end_pos = utf8.offset(s, n, i)
```

This is particularly useful when working with multi-byte UTF-8 characters, as it eliminates the need to calculate the character's ending byte position separately.

### 5. Float Printing Precision

**IMPROVED**: Floats are now printed in decimal with **enough digits to be read back correctly**.

This ensures round-trip accuracy when converting between float values and their string representations, addressing a long-standing precision issue.

### 6. More Constructor Levels

**ENHANCED**: Lua 5.5 allows for **more levels for constructors**.

This feature increases the depth of table constructor nesting that can be handled by the compiler.

______________________________________________________________________

## Memory and Performance Improvements

### 7. Compact Arrays (60% Memory Reduction)

**MAJOR IMPROVEMENT**: Large arrays now use approximately **60% less memory**.

**Implementation**: Uses "reflected arrays" architecture:

- Array of TValue is represented by an inverted array of values followed by an array of tags
- Single pointer points to the junction of two arrays for efficient access
- Addresses the padding waste problem where alignment restrictions typically waste 40% of memory

**Performance**: Negligible overhead (2% for certain access patterns, 6% for others) for significant memory savings.

### 8. Incremental Major Garbage Collection

**ENHANCED**: Major garbage collections are now done **incrementally**.

This improves pause times by breaking up major collection cycles into smaller steps interleaved with program execution, complementing the existing generational GC mode introduced in Lua 5.4.

**GC Modes**:

- **Incremental mode**: Mark-and-sweep collection in small steps
- **Generational mode**: Frequent minor collections for recent objects, with occasional major collections

______________________________________________________________________

## C API Enhancements

### 9. External Strings

**NEW**: Support for **external strings** that use memory not managed by Lua.

This allows embedding of string data from non-Lua memory pools, useful for interop scenarios or memory-constrained environments.

```c
LUA_API const char *(lua_pushexternalstring) (lua_State *L,
    const char *s, size_t len, lua_Alloc falloc, void *ud);
```

### 10. New C API Functions

#### luaL_openselectedlibs

Provides a way to **selectively open standard libraries**, addressing the long-standing need for sandboxing. Previously, developers had to use `luaL_requiref` to avoid loading unwanted libraries like IO and OS modules.

#### luaL_makeseed

A new auxiliary function for seed generation.

#### lua_numbertocstring

Converts a number at a given stack index to a C string representation:

```c
#define LUA_N2SBUFFSZ 64
LUA_API unsigned (lua_numbertocstring) (lua_State *L, int idx, char *buff);
```

Buffer size constant is 64 bytes.

### 11. Static (Fixed) Binaries

**NEW**: When loading a binary chunk in memory, Lua can **reuse its original memory** in some internal structures.

This optimization reduces memory allocation overhead when loading precompiled bytecode.

### 12. String Reuse Optimizations

**IMPROVED**:

- **Dump and undump reuse all strings**: Bytecode serialization/deserialization now shares string instances
- **Auxiliary buffer optimization**: Reuses buffer when creating final string

______________________________________________________________________

## Interactive Interpreter Improvements

### 13. Dynamic Readline Loading

**IMPROVED**: `lua.c` now loads 'readline' **dynamically** at runtime.

Previously, readline support required:

- Building with specific make target (`make linux-readline`)
- Installing readline development packages at compile time (libreadline-dev or readline-devel)

Now the interpreter can detect and use readline at runtime if available, without requiring it to be linked at compile time.

______________________________________________________________________

## Reserved Keywords (Complete List for 5.5)

```
and       break     do        else      elseif    end
false     for       function  global    goto      if
in        local     nil       not       or        repeat
return    then      true      until     while
```

Note: `global` is new in 5.5.

______________________________________________________________________

## Incompatibilities and Breaking Changes

### Confirmed Breaking Changes:

1. **For-loop variables are now read-only** - Code that modifies loop variables will need updates
1. **`global` is now a reserved keyword** - Code using 'global' as an identifier may need updates (compatibility mode available)

### Compatibility Philosophy:

According to the Lua FAQ: "If you're concerned with incompatibilities, you shouldn't, because we make every effort to avoid introducing any incompatibilities. When incompatibilities are unavoidable, previous code is usually supported unmodified, possibly by building Lua with a suitable compilation flag."

______________________________________________________________________

## Summary of Changes from Lua 5.4

### Language Features:

- Global variable declarations with `global` keyword
- Read-only for-loop variables (breaking change)
- Float printing with round-trip precision
- More constructor nesting levels

### Standard Library:

- `table.create` function for pre-allocation
- Enhanced `utf8.offset` returning character end position

### Performance & Memory:

- 60% memory reduction for large arrays
- Incremental major garbage collection
- Optimized string reuse in bytecode operations

### C API:

- External string support (`lua_pushexternalstring`)
- `luaL_openselectedlibs` for selective library loading
- `luaL_makeseed` for seed generation
- `lua_numbertocstring` for number-to-string conversion

### Implementation:

- Static binary support with memory reuse
- Dynamic readline loading in interpreter
- Auxiliary buffer optimizations

______________________________________________________________________

## Development Information

### Repository:

- Official mirror: [github.com/lua/lua](https://github.com/lua/lua)
- **Note**: The repository is mirrored irregularly; do NOT send pull requests

### Communication:

- All communication should go through the Lua mailing list: [lua-l on Google Groups](https://groups.google.com/g/lua-l)
- Bug reports and feature discussions happen on the mailing list

### License:

Lua 5.5 continues to use the **MIT license**, permitting any use including commercial applications at no cost, requiring only proper attribution.

______________________________________________________________________

## Timeline

- **June 28, 2025**: Lua 5.5.0 (beta) released
- **November 15, 2025**: Lua 5.5.0 (rc1) released - entered final release-candidate cycle
- **December 1, 2025**: Lua 5.5.0 (rc2) released - latest work version
- **Expected**: Official 5.5.0 release (date TBD)

______________________________________________________________________

## Documentation Links

- **Work area**: [lua.org/work/](https://www.lua.org/work/)
- **Reference Manual**: [lua.org/work/doc/manual.html](https://www.lua.org/work/doc/manual.html)
- **Readme**: [lua.org/work/doc/readme.html](https://www.lua.org/work/doc/readme.html)

______________________________________________________________________

## Sources

- [Lua: version history](https://www.lua.org/versions.html)
- [Lua: work area](https://www.lua.org/work/)
- [Lua 5.5 readme](https://www.lua.org/work/doc/readme.html)
- [Lua 5.5 Reference Manual](https://www.lua.org/work/doc/manual.html)
- [Lua: recent changes](https://www.lua.org/recent.html)
- [[ANN] Lua 5.5.0 (beta) now available](https://groups.google.com/g/lua-l/c/N1MMWqG4Ad0)
- [Lua 5.5.0 (Beta) Released | Hacker News](https://news.ycombinator.com/item?id=44426136)
- [GitHub - lua/lua](https://github.com/lua/lua)
- [What's the rationale behind making for loop variables constant in 5.5?](https://groups.google.com/g/lua-l/c/SlAG5QfpTac)
- [Compact Representations for Arrays in Lua (Research Paper)](https://sol.sbc.org.br/index.php/sblp/article/download/30252/30059/)
