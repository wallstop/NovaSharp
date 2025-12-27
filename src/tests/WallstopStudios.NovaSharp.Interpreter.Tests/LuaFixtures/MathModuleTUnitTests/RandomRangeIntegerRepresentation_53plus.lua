-- Tests that math.random(m, n) requires integer representation for both arguments in Lua 5.3+
-- Per Lua 5.3 manual §6.7: math.random arguments must have integer representation

-- This should error with "number has no integer representation" in 5.3+
-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs
-- @test: MathModuleTUnitTests.RandomRangeIntegerRepresentation_53plus
math.random(1.5, 10)
print("ERROR: Should have thrown")
