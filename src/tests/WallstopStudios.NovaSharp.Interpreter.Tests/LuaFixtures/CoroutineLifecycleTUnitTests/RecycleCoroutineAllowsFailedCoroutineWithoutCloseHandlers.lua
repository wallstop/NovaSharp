-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:165
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineAllowsFailedCoroutineWithoutCloseHandlers
function failingWithoutClose()
                    error('failure')
                end

                function replacement()
                    return 'recycled'
                end

                local task = coroutine.create(failingWithoutClose)
                local resumed, resumeError = coroutine.resume(task)
                print('resume', resumed, resumeError)
