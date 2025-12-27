-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:472
-- @test: MathVersionCompatibilityTUnitTests.CallingMathToIntegerInPreLua53ThrowsError
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: math.tointeger (5.3+)
return math.tointeger(42)
