-- @lua-versions: 5.1, 5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:200
-- @test: MathVersionCompatibilityTUnitTests.MathUltShouldBeNilInPreLua53
-- @compat-notes: Test targets Lua 5.1
return math.ult
