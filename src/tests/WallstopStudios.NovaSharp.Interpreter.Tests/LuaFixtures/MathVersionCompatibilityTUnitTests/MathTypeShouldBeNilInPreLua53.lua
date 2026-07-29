-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\MathVersionCompatibilityTUnitTests.cs:37
-- @test: MathVersionCompatibilityTUnitTests.MathTypeShouldBeNilInPreLua53
-- Test targets Lua 5.2+
return math.type
