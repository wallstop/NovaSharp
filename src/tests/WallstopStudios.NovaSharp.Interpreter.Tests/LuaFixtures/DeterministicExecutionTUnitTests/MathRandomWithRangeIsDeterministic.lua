-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:319
-- @test: DeterministicExecutionTUnitTests.MathRandomWithRangeIsDeterministic
-- @compat-notes: Lua 5.3+: bitwise operators
local values = {}
                for i = 1, 10 do
                    values[i] = math.random(1, 100)
                end
                return table.concat(values, ',')
