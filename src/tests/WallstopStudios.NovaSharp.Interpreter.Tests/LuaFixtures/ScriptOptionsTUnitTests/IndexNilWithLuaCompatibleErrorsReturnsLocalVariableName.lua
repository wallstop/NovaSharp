-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptOptionsTUnitTests.cs:192
-- @test: ScriptOptionsTUnitTests.IndexNilWithLuaCompatibleErrorsReturnsLocalVariableName
local x = nil; x.foo = 1
