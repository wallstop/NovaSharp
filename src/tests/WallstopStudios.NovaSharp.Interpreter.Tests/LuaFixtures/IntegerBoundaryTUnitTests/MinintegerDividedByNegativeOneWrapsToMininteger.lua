-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Compatibility/IntegerBoundaryTUnitTests.cs
-- @test: IntegerBoundaryTUnitTests.MinintegerDividedByNegativeOneWrapsToMininteger
-- @compat-notes: Lua 5.3+: mininteger // -1 wraps to mininteger due to two's complement overflow.
-- In two's complement, -mininteger overflows back to mininteger since maxinteger = mininteger - 1.
return math.mininteger // -1
