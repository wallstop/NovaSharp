-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:269
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesOwnership
function gen() return 'a' end
