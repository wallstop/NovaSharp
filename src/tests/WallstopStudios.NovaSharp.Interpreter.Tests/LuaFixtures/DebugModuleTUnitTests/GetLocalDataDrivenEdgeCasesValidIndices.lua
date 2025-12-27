-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetLocalDataDrivenEdgeCases
-- @compat-notes: Tests debug.getlocal with valid indices

-- Test: debug.getlocal with valid indices returns name and value
local function sample(arg1, arg2, arg3)
  local loc1 = 'local1'
  local loc2 = 'local2'

  -- Test first argument
  local name1, value1 = debug.getlocal(1, 1)
  assert(type(name1) == 'string', "Local name at index 1 should be a string")

  -- Test local variable
  local name4, value4 = debug.getlocal(1, 4)
  assert(type(name4) == 'string', "Local name at index 4 should be a string")

  return true
end
return sample('a', 'b', 'c')