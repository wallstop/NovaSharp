-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:403
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextArgsThrowsWhenContextNull
-- @compat-notes: Test targets Lua 5.1
return function() return 1 end
