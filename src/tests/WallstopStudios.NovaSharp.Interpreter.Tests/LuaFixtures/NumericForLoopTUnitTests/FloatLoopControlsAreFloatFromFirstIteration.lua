-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:502
-- @test: NumericForLoopTUnitTests.FloatLoopControlsAreFloatFromFirstIteration
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.3+: math.type (5.3+)
local t = {}
                  for i = 1, 3, 1.0 do t[#t + 1] = math.type(i) end
                  return table.concat(t, ',')
