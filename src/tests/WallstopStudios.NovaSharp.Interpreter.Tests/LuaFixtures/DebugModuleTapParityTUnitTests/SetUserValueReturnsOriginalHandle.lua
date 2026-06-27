-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:204
-- @test: DebugModuleTapParityTUnitTests.SetUserValueReturnsOriginalHandle
-- Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+)
local payload = { flag = true }
                local assigned = debug.setuservalue(handle, payload)
                return assigned == handle, debug.getuservalue(handle) == payload
