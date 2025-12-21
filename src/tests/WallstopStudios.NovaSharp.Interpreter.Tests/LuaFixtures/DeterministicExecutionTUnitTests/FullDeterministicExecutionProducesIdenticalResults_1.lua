-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/DeterministicExecutionTUnitTests.cs:502
-- @test: DeterministicExecutionTUnitTests.FullDeterministicExecutionProducesIdenticalResults
-- @compat-notes: Lua 5.3+: bitwise OR
local results = {}
                results[1] = math.random()
                results[2] = math.random(1, 1000)
                results[3] = os.clock()
                results[4] = os.time()
                return table.concat(results, '|')
