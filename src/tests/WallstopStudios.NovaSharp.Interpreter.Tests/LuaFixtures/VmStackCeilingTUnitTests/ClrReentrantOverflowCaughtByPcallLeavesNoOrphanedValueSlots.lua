-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/VmStackCeilingTUnitTests.cs:230
-- @test: VmStackCeilingTUnitTests.ClrReentrantOverflowCaughtByPcallLeavesNoOrphanedValueSlots
-- Uses injected host functions: reenter, probe_before, probe_after
local function f() return reenter(f) end
                probe_before()
                local ok, err = pcall(reenter, f)
                assert(ok == false and tostring(err):find('stack overflow') ~= nil)
                probe_after()
