-- Tests that math.randomseed(x) requires integer representation in Lua 5.4+
-- Per Lua 5.4 manual §6.7: math.randomseed argument must have integer representation
-- NOTE: Lua 5.3 does NOT require integer representation for randomseed

-- @lua-versions: 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomseedIntegerRepresentation_54plus
math.randomseed(1.5)
print("ERROR: Should have thrown")
