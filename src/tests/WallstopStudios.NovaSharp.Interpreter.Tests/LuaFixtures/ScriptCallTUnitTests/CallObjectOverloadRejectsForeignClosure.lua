-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:435
-- @test: ScriptCallTUnitTests.CallObjectOverloadRejectsForeignClosure
-- @compat-notes: Test targets Lua 5.1
function noop() return 1 end
