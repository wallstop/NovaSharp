-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:863
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithInvalidSlotReturnsNil
-- Uses injected variable: ud
local ret = debug.setuservalue(ud, { test = 'value' }, "2")
                local orderOk, orderError = pcall(
                    debug.setuservalue,
                    'not userdata',
                    {},
                    1.5
                )
                return ret, orderOk, orderError
