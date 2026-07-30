-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:825
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithFourthArgumentFromDifferentScriptThrows
-- Compatibility notes: Test targets Lua 5.1
return function(a, b, c, d) return d end
