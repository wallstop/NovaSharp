-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\DataTypes\TailCallTUnitTests.cs:115
-- @test: TailCallTUnitTests.TailCallRequestPropagatesAcrossClrCallback
function getResult(x)
                    return 156 * x
                end

                return clrtail(9)
