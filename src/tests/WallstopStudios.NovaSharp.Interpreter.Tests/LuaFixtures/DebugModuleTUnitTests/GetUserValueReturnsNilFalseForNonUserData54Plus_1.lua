-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:528
-- @test: DebugModuleTUnitTests.GetUserValueReturnsNilFalseForNonUserData54Plus
-- Test targets Lua 5.4+
local results = table.pack(debug.getuservalue('string'))
                return results.n, results[1], results[2]
