-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptOptionsTUnitTests.cs:248
-- @test: ScriptOptionsTUnitTests.RecycleCoroutineValidatesFunctionType
function gen() return 'done' end
