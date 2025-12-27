-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:506
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithContextFromDifferentScriptThrows
-- @compat-notes: Test targets Lua 5.1
return function() return 1 end
