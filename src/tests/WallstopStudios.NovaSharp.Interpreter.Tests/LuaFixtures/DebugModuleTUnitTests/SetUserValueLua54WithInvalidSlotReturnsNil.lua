-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:703
-- @test: DebugModuleTUnitTests.SetUserValueLua54WithInvalidSlotReturnsNil
-- @compat-notes: Test targets Lua 5.4+; Lua 5.2+: debug.setuservalue (5.2+)
local ret = debug.setuservalue(ud, { test = 'value' }, 2)
                return ret
