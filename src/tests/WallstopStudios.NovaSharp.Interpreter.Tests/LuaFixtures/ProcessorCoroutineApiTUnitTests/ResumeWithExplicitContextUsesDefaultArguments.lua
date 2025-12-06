-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:210
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithExplicitContextUsesDefaultArguments
return function() return 5 end
