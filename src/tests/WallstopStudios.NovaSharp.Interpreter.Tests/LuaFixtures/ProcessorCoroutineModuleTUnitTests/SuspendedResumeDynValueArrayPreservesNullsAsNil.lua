-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:473
-- @test: ProcessorCoroutineModuleTUnitTests.SuspendedResumeDynValueArrayPreservesNullsAsNil
-- Compatibility notes: Test targets Lua 5.1
return function()
                    local a, b, c = coroutine.yield('pause')
                    return select('#', a, b, c), a == nil, b, c == nil
                end
