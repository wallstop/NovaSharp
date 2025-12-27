-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- @compat-notes: Tests 'l' what flag for line info

-- Test: debug.getinfo with 'l' flag populates currentline field
local function sample(a, b)
    local c = a + b
    return c
end
local info = debug.getinfo(sample, 'l')
assert(info.currentline ~= nil, "currentline field should be populated with 'l' flag")
return true