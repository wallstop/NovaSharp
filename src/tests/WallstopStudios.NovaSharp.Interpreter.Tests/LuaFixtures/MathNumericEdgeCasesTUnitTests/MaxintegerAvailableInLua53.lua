-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathNumericEdgeCasesTUnitTests.cs:84
-- @test: MathNumericEdgeCasesTUnitTests.MaxintegerAvailableInLua53
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: math.maxinteger (5.3+)
return math.maxinteger
