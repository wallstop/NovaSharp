-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Debugging/DebuggerResumeHelpersTUnitTests.cs:49
-- @test: DebuggerResumeHelpersTUnitTests.StepInThenStepOutBreaksOnExpectedLines
function callee()
  local inside = 42
  return inside
end

function caller()
  local before = callee()
  local after = before + 1
  return after
end

return caller()
