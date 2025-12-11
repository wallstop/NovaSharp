-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\Execution\ScriptOptionsTUnitTests.cs:290
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineReusesBuffers
function gen() return 'original' end
