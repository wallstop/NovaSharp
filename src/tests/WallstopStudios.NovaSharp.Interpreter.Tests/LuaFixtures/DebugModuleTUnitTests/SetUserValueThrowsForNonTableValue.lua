-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:529
-- @test: DebugModuleTUnitTests.SetUserValueThrowsForNonTableValue
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.setuservalue (5.2+)
local ok, err = pcall(function() debug.setuservalue(ud, 'not a table') end)
                return ok, err
