-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:763
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewHandlesLuaNoReturnTrailingArgument
-- Compatibility notes: Test targets Lua 5.1; Uses injected variable: callback
local function values()
                end

                return callback(10, values())
