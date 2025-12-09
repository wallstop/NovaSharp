-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:484
-- @test: MathModuleTUnitTests.FloorResultCanBeUsedInStringFormat
-- @compat-notes: Lua 5.3+: string.format('%d', x) throws "number has no integer representation"
-- when x exceeds the integer range. math.maxinteger + 0.5 after floor() is still > maxinteger.
-- BUG: NovaSharp incorrectly allows this with overflow wrapping instead of throwing.
return string.format('%d', math.floor(math.maxinteger + 0.5))
