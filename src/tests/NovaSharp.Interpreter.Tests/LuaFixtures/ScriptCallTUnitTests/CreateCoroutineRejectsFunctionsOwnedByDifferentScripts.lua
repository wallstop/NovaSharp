-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ScriptCallTUnitTests.cs:333
-- @test: ScriptCallTUnitTests.CreateCoroutineRejectsFunctionsOwnedByDifferentScripts
return function() end
