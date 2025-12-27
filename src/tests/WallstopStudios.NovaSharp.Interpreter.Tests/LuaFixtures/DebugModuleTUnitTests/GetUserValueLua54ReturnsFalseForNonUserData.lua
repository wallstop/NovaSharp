-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:652
-- @test: DebugModuleTUnitTests.GetUserValueLua54ReturnsFalseForNonUserData
-- @compat-notes: Test targets Lua 5.4+; Lua 5.2+: debug.getuservalue (5.2+); Uses injected variable: userdata
local val, hasVal = debug.getuservalue('not userdata', 1)
                return val, hasVal
