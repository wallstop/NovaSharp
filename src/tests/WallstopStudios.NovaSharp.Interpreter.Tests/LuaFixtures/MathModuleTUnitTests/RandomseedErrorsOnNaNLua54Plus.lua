-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:910
-- @test: MathModuleTUnitTests.RandomseedErrorsOnNaNLua54Plus
-- @compat-notes: Test targets Lua 5.1
math.randomseed(0/0)
