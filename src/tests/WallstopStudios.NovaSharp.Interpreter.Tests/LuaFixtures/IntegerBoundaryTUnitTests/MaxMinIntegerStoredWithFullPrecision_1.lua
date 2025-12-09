-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:731
-- @test: IntegerBoundaryTUnitTests.MaxMinIntegerStoredWithFullPrecision
-- @compat-notes: Lua 5.3+: math.mininteger (5.3+)
return math.mininteger
