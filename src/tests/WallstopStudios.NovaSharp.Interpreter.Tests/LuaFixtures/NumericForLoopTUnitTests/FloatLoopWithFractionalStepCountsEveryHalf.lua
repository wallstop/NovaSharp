-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:170
-- @test: NumericForLoopTUnitTests.FloatLoopWithFractionalStepCountsEveryHalf
-- Compatibility notes: Test targets Lua 5.3+
local n = 0
                  for i = 1, 3, 0.5 do n = n + 1 end
                  return n
