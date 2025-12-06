-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:329
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextObjectArgsThrowsWhenArgsNull
return function() return 1 end
