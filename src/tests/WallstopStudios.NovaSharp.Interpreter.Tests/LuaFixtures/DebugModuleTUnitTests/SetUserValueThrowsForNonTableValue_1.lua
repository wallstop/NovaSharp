-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:554
-- @test: DebugModuleTUnitTests.SetUserValueThrowsForNonTableValue
-- Uses injected variable: ud
local ok, result = pcall(function()
                    return debug.setuservalue(ud, 'not a table')
                end)
                local value = debug.getuservalue(ud)
                return ok, result, value
