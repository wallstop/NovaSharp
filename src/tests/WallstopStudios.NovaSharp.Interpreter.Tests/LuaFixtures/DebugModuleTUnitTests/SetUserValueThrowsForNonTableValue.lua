-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:293
-- @test: DebugModuleTUnitTests.SetUserValueThrowsForNonTableValue
-- @compat-notes: Lua 5.3+: bitwise operators; Lua 5.2+: debug.setuservalue (5.2+)
local ok, err = pcall(function() debug.setuservalue(ud, 'not a table') end)
                return ok, err
