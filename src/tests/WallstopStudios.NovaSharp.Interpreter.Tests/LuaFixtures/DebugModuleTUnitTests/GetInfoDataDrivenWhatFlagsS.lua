-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- Tests 'S' what flag for source fields

-- Test: debug.getinfo with 'S' flag populates source field
local function sample(a, b)
    local c = a + b
    return c
end
local info = debug.getinfo(sample, 'S')
assert(info.source ~= nil, "source field should be populated with 'S' flag")
return true