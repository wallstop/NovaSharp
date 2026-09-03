-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:136
-- @test: NumericForLoopTUnitTests.FloatLoopWithFloatInitIteratesFractionalValues
local n = 0
                  local last
                  for i = 1.5, 3 do n = n + 1 last = i end
                  return n, last
