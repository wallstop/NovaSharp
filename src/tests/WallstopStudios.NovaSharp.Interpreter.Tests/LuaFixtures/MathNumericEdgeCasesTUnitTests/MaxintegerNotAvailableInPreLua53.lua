-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathNumericEdgeCasesTUnitTests.cs:93
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerNotAvailableInPreLua53
-- @compat-notes: Test targets Lua 5.1; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger
