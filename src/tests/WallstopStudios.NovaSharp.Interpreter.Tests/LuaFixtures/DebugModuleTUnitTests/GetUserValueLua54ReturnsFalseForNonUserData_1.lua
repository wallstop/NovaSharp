-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:757
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForNonUserData
-- Test targets Lua 5.4+
local val, hasVal = debug.getuservalue('not userdata', 1)
                local stringOk, stringVal, stringHasVal = pcall(
                    debug.getuservalue,
                    'not userdata',
                    "1"
                )
                local fractionOk, fractionError = pcall(
                    debug.getuservalue,
                    'not userdata',
                    1.5
                )
                return val, hasVal, stringOk, stringVal, stringHasVal,
                    fractionOk, fractionError
