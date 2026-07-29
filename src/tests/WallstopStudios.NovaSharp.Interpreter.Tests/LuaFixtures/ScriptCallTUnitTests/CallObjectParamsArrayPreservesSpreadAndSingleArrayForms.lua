-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:223
-- @test: ScriptCallTUnitTests.CallObjectParamsArrayPreservesSpreadAndSingleArrayForms
function capture(...) return select('#', ...), ... end
