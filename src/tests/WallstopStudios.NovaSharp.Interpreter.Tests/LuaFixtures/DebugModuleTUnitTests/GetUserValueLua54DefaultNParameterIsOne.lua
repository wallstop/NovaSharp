-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:724
-- @test: DebugModuleTUnitTests.GetUserValueLua54DefaultNParameterIsOne
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+)
debug.setuservalue(ud, { label = 'default' })
                local val, hasVal = debug.getuservalue(ud)
                return val and val.label, hasVal
