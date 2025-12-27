-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:195
-- @test: MathVersionCompatibilityTUnitTests.MathUltShouldBeNilInPreLua53
-- @compat-notes: Test targets Lua 5.2+
return math.ult
