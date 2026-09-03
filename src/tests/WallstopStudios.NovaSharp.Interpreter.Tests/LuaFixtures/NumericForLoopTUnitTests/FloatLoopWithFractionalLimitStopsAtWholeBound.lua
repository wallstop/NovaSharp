-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:154
-- @test: NumericForLoopTUnitTests.FloatLoopWithFractionalLimitStopsAtWholeBound
local n = 0
                  local last
                  for i = 3, 1.5, -1 do n = n + 1 last = i end
                  return n, last
