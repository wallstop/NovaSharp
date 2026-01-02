-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- @compat-notes: Tests 'f' what flag for function reference

-- Test: debug.getinfo with 'f' flag populates func field
local function sample(a, b)
  local c = a + b
  return c
end
local info = debug.getinfo(sample, 'f')
assert(info.func ~= nil, "func field should be populated with 'f' flag")
return true