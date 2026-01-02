-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaRandomParityTUnitTests.cs:462
-- @test: LuaRandomParityTUnitTests.MathRandomIntegerRangeWorksForAllVersions
-- @compat-notes: Test targets Lua 5.2+
return math.random(100)
