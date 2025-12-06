-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:316
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithObjectArgsThrowsWhenArgsNull
return function() return 1 end
