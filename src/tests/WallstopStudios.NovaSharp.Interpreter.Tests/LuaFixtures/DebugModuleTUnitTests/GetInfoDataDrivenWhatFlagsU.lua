-- @lua-versions: all
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetInfoDataDrivenWhatFlags
-- Tests 'u' what flag for upvalue count

-- Test: debug.getinfo with 'u' flag populates nups field
local function sample(a, b)
    local c = a + b
    return c
end
local info = debug.getinfo(sample, 'u')
assert(info.nups ~= nil, "nups field should be populated with 'u' flag")
return true