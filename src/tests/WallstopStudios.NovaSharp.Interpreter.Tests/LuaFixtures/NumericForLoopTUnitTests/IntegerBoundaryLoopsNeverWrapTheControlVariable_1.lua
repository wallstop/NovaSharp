-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:191
-- @test: NumericForLoopTUnitTests.IntegerBoundaryLoopsNeverWrapTheControlVariable
-- Compatibility notes: reference Lua 5.3 loops forever on ranges reaching the integer extremes
local t = {}
                  for i = math.mininteger + 2, math.mininteger, -1 do t[#t + 1] = i end
                  return table.concat(t, ',')
