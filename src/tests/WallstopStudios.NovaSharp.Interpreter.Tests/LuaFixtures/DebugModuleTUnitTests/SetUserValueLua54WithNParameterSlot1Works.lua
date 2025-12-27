-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:675
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithNParameterSlot1Works
-- @compat-notes: Test targets Lua 5.4+; Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+)
local payload = { test = 'value' }
                local ret = debug.setuservalue(ud, payload, 1)
                local val, hasVal = debug.getuservalue(ud, 1)
                return ret == ud, val and val.test, hasVal
