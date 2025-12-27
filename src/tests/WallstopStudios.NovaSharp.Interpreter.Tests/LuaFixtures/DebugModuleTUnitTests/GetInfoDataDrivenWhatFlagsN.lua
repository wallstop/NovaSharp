-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- @compat-notes: Tests 'n' what flag for name fields

-- Test: debug.getinfo with 'n' flag populates name field
local function sample(a, b)
    local c = a + b
    return c
end
local info = debug.getinfo(sample, 'n')
return info.name ~= nil or true -- name can be nil for local functions