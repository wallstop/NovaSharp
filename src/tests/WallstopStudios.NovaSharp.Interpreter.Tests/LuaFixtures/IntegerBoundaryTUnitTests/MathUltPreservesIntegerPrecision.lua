-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/IntegerBoundaryTUnitTests.cs:430
-- @test: IntegerBoundaryTUnitTests.MathUltPreservesIntegerPrecision
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger
