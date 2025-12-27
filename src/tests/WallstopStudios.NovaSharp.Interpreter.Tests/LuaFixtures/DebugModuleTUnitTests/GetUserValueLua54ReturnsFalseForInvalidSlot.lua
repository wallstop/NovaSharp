-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:629
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForInvalidSlot
-- @compat-notes: Test targets Lua 5.4+; Lua 5.2+: debug.getuservalue (5.2+)
local val, hasVal = debug.getuservalue(ud, 2)
                return val, hasVal
