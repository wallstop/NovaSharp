-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleVersionParityTUnitTests.cs:91
-- @test: LoadModuleVersionParityTUnitTests.LoadRejectsStringArgumentInLua51
-- @compat-notes: In Lua 5.1, load() only accepts reader functions, not strings

-- Test: load() should reject string arguments in Lua 5.1
-- Reference: Lua 5.1 Reference Manual §5.1 - load (func [, chunkname])

-- This should error because Lua 5.1 load() expects a function, not a string
local fn, err = load("return 42")

-- If we get here without error, something is wrong
-- The error message in Lua 5.1 is typically about expecting function
error("load should have errored when given a string in Lua 5.1")
