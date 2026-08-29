-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTapParityTUnitTests.cs:230
-- @test: DebugModuleTapParityTUnitTests.SetUserValueRejectsNonTablesWithLuaMessage
-- Uses injected variable: handle
local ok, valueOrError = pcall(function()
                    return debug.setuservalue(handle, true)
                end)
                return ok, valueOrError, debug.getuservalue(handle)
