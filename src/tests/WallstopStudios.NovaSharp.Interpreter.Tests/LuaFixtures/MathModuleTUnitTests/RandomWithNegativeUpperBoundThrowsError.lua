-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:1038
-- @test: MathModuleTUnitTests.RandomWithNegativeUpperBoundThrowsError
-- @compat-notes: Test targets Lua 5.4+
return math.random(-5)
