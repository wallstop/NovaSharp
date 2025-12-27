-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathVersionCompatibilityTUnitTests.cs:445
-- @test: MathVersionCompatibilityTUnitTests.CallingMathTypeInPreLua53ThrowsError
-- @compat-notes: Test targets Lua 5.2+; Lua 5.3+: math.type (5.3+)
return math.type(5)
