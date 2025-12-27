-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:229
-- @test: DebugModuleTapParityTUnitTests.SetUserValueRejectsNonTablesWithLuaMessage
-- @compat-notes: Lua 5.2+: debug.setuservalue (5.2+)
local ok, err = pcall(function()
                    debug.setuservalue(handle, true)
                end)
                return ok, err
