-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenCombinedWhatFlags
-- @compat-notes: Tests combined 'nS' what flags

-- Test: debug.getinfo with combined 'nS' flags populates name and source fields
local function sample(a, b)
  local c = a + b
  return c
end
local info = debug.getinfo(sample, 'nS')
assert(info.source ~= nil, "source field should be populated with 'nS' flags")
-- name can be nil for local functions, so we just check source
return true