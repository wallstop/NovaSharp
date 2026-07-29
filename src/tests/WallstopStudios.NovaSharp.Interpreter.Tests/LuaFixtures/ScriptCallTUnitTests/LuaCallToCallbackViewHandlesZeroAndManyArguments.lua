-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptCallTUnitTests.cs:695
-- @test: ScriptCallTUnitTests.LuaCallToCallbackViewHandlesZeroAndManyArguments
-- Compatibility notes: Test targets Lua 5.5+; Uses injected variable: callback
callback(); return callback(1, 2, 3, 4, 5)
