-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:235
-- @test: NumericForLoopTUnitTests.BoundaryLoopsWithMaximalStepsStopBeforeWrapping
-- Compatibility notes: reference Lua 5.3 loops forever on ranges reaching the integer extremes
local t = {}
                  for i = 0, math.maxinteger, math.maxinteger do t[#t + 1] = i end
                  return table.concat(t, ',')
