-- @lua-versions: 5.1, 5.2, 5.3
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:935
-- @test: MathModuleTUnitTests.RandomWithZeroUpperBoundThrowsErrorLua51To53
-- @compat-notes: Test targets Lua 5.1
return math.random(0)
