-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/LoadModuleTUnitTests.cs
-- @test: LoadModuleTUnitTests.LoadRejectsNumberArgumentInLua51
-- @compat-notes: Lua 5.1 load() only accepts functions, not strings or numbers

-- Test: load() should reject numbers in Lua 5.1
-- Reference: Lua 5.1's load() only accepts reader functions
load(123)
print("ERROR: Should have thrown")
