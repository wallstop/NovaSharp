-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptOptionsTUnitTests.cs:209
-- @test: ScriptOptionsTUnitTests.IndexNilWithLuaCompatibleErrorsReturnsGlobalVariableName
undeclared.field = 1
