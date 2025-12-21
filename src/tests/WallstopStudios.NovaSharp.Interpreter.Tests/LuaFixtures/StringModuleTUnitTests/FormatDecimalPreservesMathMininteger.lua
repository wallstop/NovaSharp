-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1990
-- @test: StringModuleTUnitTests.FormatDecimalPreservesMathMininteger
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+)
return string.format('%d', math.mininteger)
