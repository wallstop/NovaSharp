-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:618
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewExposesContiguousSpan
-- Compatibility notes: Test targets Lua 5.4+; Uses injected variable: callback
return callback(10, 20, 30)
