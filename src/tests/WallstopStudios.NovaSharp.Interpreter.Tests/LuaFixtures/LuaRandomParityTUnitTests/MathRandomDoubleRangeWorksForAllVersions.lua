-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:487
-- @test: LuaRandomParityTUnitTests.MathRandomDoubleRangeWorksForAllVersions
-- @compat-notes: Test targets Lua 5.2+
return math.random()
