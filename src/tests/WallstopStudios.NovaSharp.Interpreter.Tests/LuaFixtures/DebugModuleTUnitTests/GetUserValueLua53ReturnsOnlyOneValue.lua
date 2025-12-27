-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:749
-- @test: DebugModuleTUnitTests.GetUserValueLua53ReturnsOnlyOneValue
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.getuservalue (5.2+); Lua 5.2+: debug.setuservalue (5.2+)
debug.setuservalue(ud, { value = 42 })
                local results = {debug.getuservalue(ud)}
                return #results, results[1] and results[1].value, results[2]
