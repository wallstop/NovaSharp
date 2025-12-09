-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorStackTraceTUnitTests.cs:21
-- @test: ProcessorStackTraceTUnitTests.CoroutineStackTraceIncludesCurrentFrames
function level3()
                    coroutine.yield('pause')
                end

                function level2()
                    level3()
                end

                function level1()
                    level2()
                end
