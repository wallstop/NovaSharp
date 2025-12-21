-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:177
-- @test: MathNumericEdgeCasesTUnitTests.MinintegerBitwiseOrWorksCorrectly
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: bitwise OR; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger | 0
