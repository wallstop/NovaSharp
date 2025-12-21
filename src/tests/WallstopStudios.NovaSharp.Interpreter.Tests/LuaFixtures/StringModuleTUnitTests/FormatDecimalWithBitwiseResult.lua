-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2389
-- @test: StringModuleTUnitTests.FormatDecimalWithBitwiseResult
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: bitwise AND; Lua 5.3+: math.maxinteger (5.3+)
return string.format('%d', math.maxinteger & math.maxinteger)
