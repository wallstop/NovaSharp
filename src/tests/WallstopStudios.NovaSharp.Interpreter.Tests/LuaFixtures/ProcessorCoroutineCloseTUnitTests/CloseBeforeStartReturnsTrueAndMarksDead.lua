-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:24
-- @test: ProcessorCoroutineCloseTUnitTests.CloseBeforeStartReturnsTrueAndMarksDead
-- @compat-notes: Test targets Lua 5.1
function ready() return 1 end
