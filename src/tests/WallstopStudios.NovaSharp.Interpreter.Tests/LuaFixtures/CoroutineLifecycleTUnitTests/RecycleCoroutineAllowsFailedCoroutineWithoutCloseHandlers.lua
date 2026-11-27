-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:165
-- @test: CoroutineLifecycleTUnitTests.RecycleCoroutineAllowsFailedCoroutineWithoutCloseHandlers
function failingWithoutClose()
                    error('failure')
                end

                function replacement()
                    return 'recycled'
                end
