-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:121
-- @test: MathVersionCompatibilityTUnitTests.MathToIntegerShouldBeNilInPreLua53
-- @compat-notes: Test targets Lua 5.2+
return math.tointeger
