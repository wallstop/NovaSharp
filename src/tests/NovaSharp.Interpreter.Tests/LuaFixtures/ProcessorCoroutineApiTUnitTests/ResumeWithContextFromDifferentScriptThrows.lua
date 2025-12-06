-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:363
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextFromDifferentScriptThrows
return function() return 1 end
