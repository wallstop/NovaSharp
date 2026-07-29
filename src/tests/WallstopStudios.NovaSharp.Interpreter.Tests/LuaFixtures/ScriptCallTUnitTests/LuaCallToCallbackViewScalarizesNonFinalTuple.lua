-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:860
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewScalarizesNonFinalTuple
-- Compatibility notes: Test targets Lua 5.1; Uses injected variable: callback
local function values()
                    return 10, 20
                end

                return callback(values(), 30)
