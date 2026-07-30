-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:838
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewHandlesClrEmptyTrailingTuple
-- Compatibility notes: Test targets Lua 5.1; Uses injected variable: callback
return callback(10, empty())
