-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:800
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewHandlesLuaSingleReturnTrailingArgument
-- Compatibility notes: Test targets Lua 5.1; Uses injected variable: callback
local function values()
                    return 20
                end

                return callback(10, values())
