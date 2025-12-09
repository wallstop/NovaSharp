-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:474
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerEqualToLiteral
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: bitwise operators; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger == 9223372036854775807
