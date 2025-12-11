-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Compatibility\IntegerBoundaryTUnitTests.cs:743
-- @test: IntegerBoundaryTUnitTests.MaxMinIntegerStoredWithFullPrecision
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: math.tointeger (5.3+); Lua 5.3+: math.mininteger (5.3+)
return math.tointeger(math.mininteger)
