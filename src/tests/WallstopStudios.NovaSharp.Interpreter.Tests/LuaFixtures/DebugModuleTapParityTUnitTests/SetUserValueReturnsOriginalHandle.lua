-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\DebugModuleTapParityTUnitTests.cs:192
-- @test: DebugModuleTapParityTUnitTests.SetUserValueReturnsOriginalHandle
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+)
local payload = { flag = true }
                local assigned = debug.setuservalue(handle, payload)
                return assigned == handle, debug.getuservalue(handle) == payload
