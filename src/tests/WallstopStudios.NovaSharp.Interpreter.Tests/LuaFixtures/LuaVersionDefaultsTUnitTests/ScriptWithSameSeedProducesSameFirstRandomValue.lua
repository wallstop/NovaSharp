-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaVersionDefaultsTUnitTests.cs:276
-- @test: LuaVersionDefaultsTUnitTests.ScriptWithSameSeedProducesSameFirstRandomValue
-- NovaSharp: unresolved C# interpolation placeholder
math.randomseed({seed})
