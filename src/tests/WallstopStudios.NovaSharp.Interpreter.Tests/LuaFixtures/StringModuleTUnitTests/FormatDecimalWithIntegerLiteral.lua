-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:1577
-- @test: StringModuleTUnitTests.FormatDecimalWithIntegerLiteral
-- @compat-notes: Test targets Lua 5.1
return string.format('%d', 9223372036854775807)
