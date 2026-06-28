-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:431
-- @test: IntegerBoundaryTUnitTests.MathUltPreservesIntegerPrecision
-- Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger
