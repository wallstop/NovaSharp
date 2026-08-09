-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptExecutionContextTUnitTests.cs:33
-- @test: ScriptExecutionContextTUnitTests.EvaluateSymbolByNameResolvesLocals
local localValue = 123
local activeNilShadowsOuter
do
    local localValue = nil
    activeNilShadowsOuter = localValue == nil
end

print("shadow", activeNilShadowsOuter, localValue)
return activeNilShadowsOuter, localValue
