-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:356
-- @test: DeterministicExecutionTUnitTests.MathRandomWithRangeIsDeterministic
local values = {}
                for i = 1, 10 do
                    values[i] = math.random(1, 100)
                end
                return table.concat(values, ',')
