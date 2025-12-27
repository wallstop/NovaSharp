-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:531
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithArgumentsFromDifferentScriptThrows
-- @compat-notes: Test targets Lua 5.1
return function(value) return value end
