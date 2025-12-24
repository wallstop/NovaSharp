-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/StringModuleTUnitTests.cs:2062
-- @test: StringModuleTUnitTests.FormatDecimalWithIntegerLiteral
-- @compat-notes: Platform-specific: 32-bit Lua builds reject this large integer; NovaSharp handles consistently
return string.format('%d', 9223372036854775807)
