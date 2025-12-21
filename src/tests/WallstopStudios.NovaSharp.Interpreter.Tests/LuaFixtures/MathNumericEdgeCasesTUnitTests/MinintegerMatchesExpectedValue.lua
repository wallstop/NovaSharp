-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:48
-- @test: MathNumericEdgeCasesTUnitTests.MinintegerMatchesExpectedValue
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.mininteger (5.3+)
return math.mininteger
