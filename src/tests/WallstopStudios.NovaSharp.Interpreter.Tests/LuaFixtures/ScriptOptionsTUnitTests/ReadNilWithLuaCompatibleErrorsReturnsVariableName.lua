-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptOptionsTUnitTests.cs:251
-- @test: ScriptOptionsTUnitTests.ReadNilWithLuaCompatibleErrorsReturnsVariableName
local x = nil; local y = x.foo
