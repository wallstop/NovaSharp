-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:192
-- @test: DebugModuleTapParityTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local payload = { flag = true }
                local assigned = debug.setuservalue(handle, payload)
                return assigned == handle, debug.getuservalue(handle) == payload
