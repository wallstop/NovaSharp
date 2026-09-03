-- @lua-versions: 5.1-5.3
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:293
-- @test: NumericForLoopTUnitTests.ZeroStepRunsZeroIterationsBeforeLua54
-- Compatibility notes: Test targets Lua 5.3+
local n = 0
                  for i = 1, 10, 0 do n = n + 1 end
                  for i = 1, 10, 0.0 do n = n + 1 end
                  return n
