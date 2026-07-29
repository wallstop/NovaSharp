-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:345
-- @test: ProcessorCoroutineApiTUnitTests.ResumeWithDynValueSpanUsesSliceAndNormalizesNull
-- Compatibility notes: Test targets Lua 5.1
return function(a, b, c) if a ~= nil then return -1 end return b + c end
