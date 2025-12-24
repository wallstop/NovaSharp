-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:876
-- @test: MathModuleTUnitTests.RandomseedErrorsOnNonIntegerArgLua54Plus
-- @compat-notes: Test targets Lua 5.1
math.randomseed(1.5)
