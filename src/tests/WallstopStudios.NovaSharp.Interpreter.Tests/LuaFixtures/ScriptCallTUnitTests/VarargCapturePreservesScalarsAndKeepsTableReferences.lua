-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:228
-- @test: ScriptCallTUnitTests.VarargCapturePreservesScalarsAndKeepsTableReferences
return function(...) return ... end
