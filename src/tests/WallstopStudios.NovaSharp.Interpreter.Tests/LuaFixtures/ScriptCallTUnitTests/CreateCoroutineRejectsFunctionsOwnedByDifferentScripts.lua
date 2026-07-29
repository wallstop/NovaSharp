-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ScriptExecution/ScriptCallTUnitTests.cs:565
-- @test: ScriptCallTUnitTests.CreateCoroutineRejectsFunctionsOwnedByDifferentScripts
-- Compatibility notes: Test targets Lua 5.1
return function() end
