-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:493
-- @test: MathVersionCompatibilityTUnitTests.CallingMathUltInPreLua53ThrowsError
-- Test targets Lua 5.2+; Lua 5.3+: math.ult (5.3+)
return math.ult(0, 1)
