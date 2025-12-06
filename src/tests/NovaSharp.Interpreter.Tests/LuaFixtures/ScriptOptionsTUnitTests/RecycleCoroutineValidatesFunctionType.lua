-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ScriptOptionsTUnitTests.cs:247
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesFunctionType
function gen() return 'done' end
