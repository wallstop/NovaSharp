-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:564
-- @test: ProcessorCoroutineModuleTUnitTests.SuspendedResumeObjectArgumentsSupportsPooledSpanSlice
return function()
                    local a, b, c, d, e, f = coroutine.yield('ready')
                    return select('#', a, b, c, d, e, f), a, b, c, d, e, f
                end
