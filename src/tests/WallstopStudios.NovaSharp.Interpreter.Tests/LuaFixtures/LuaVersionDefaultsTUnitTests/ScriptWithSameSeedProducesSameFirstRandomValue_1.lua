-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionDefaultsTUnitTests.cs:277
-- @test: LuaVersionDefaultsTUnitTests.ScriptWithSameSeedProducesSameFirstRandomValue
-- NovaSharp: unresolved C# interpolation placeholder; Test targets Lua 5.2+
math.randomseed({seed})
