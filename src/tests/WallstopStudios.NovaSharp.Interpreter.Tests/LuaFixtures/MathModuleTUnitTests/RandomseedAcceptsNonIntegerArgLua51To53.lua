-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/MathModuleTUnitTests.cs:969
-- @test: MathModuleTUnitTests.RandomseedAcceptsNonIntegerArgLua51To53
-- @compat-notes: Test targets Lua 5.1
math.randomseed(1.5); return true
