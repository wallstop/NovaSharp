-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:592
-- @test: DebugModuleTUnitTests.SetUserValueThrowsWhenNoValueProvided
-- Uses injected variable: ud
local seeded = { value = 42 }
                debug.setuservalue(ud, seeded)
                local ok, valueOrError = pcall(function()
                    return debug.setuservalue(ud)
                end)
                return ok, valueOrError, debug.getuservalue(ud), seeded
