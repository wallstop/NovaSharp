-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:60
-- @test: MathVersionCompatibilityTUnitTests.MathTypeAvailableInLua53Plus
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: math.type (5.3+)
return math.type(5.5)
