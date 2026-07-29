-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:718
-- @test: TailCallTUnitTests.ArgumentViewTailCallContinuationKeepsSpanBackedReturnValue
function returnValue()
                    return 41
                end

                return clrtail()
