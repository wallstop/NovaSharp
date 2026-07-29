-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:654
-- @test: ScriptCallTUnitTests.LuaCallToNoContextCallbackViewExposesContiguousSpan
-- Compatibility notes: Uses injected variable: callback
return callback(10, 20, 30)
