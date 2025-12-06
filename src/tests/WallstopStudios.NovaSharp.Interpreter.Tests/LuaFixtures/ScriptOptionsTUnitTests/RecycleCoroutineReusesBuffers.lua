-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ScriptOptionsTUnitTests.cs:289
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineReusesBuffers
function gen() return 'original' end
