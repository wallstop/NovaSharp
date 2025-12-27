-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:858
-- @test: MathModuleTUnitTests.RandomErrorsOnNegativeInfinityLua51And52
-- @compat-notes: Test targets Lua 5.1
return math.random(-1/0)
