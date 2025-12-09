-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:483
-- @test: MathNumericEdgeCasesTUnitTests.MinintegerEqualToLiteral
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger == -9223372036854775808
