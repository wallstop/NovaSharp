-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ScriptOptionsTUnitTests.cs:268
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesOwnership
function gen() return 'a' end
