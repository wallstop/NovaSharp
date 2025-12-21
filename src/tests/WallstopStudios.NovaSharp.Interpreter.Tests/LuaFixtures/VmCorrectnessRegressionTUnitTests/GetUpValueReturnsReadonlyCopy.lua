-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/VmCorrectnessRegressionTUnitTests.cs:29
-- @test: VmCorrectnessRegressionTUnitTests.GetUpValueReturnsReadonlyCopy
local x = 10
                return function()
                    return x
                end
