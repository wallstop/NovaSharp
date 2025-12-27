-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs
-- @test: DebugModuleTUnitTests.GetLocalDataDrivenEdgeCases
-- @compat-notes: Tests debug.getlocal with invalid indices (0, negative, beyond bounds)

-- Test: debug.getlocal with invalid indices returns nil
local function sample(arg1, arg2, arg3)
    local loc1 = 'local1'

    -- Test zero index
    local name0 = debug.getlocal(1, 0)
    assert(name0 == nil, "Index 0 should return nil")

    -- Test negative index
    local nameNeg = debug.getlocal(1, -1)
    assert(nameNeg == nil, "Negative index should return nil")

    -- Test beyond bounds
    local name100 = debug.getlocal(1, 100)
    assert(name100 == nil, "Index beyond bounds should return nil")

    return true
end
return sample('a', 'b', 'c')