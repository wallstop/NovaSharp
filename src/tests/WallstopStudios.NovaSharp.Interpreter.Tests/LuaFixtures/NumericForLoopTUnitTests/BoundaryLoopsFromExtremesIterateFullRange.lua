-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:212
-- @test: NumericForLoopTUnitTests.BoundaryLoopsFromExtremesIterateFullRange
-- Compatibility notes: reference Lua 5.3 loops forever on ranges reaching the integer extremes
local t = {}
                  for i = math.mininteger, math.mininteger + 3 do t[#t + 1] = i end
                  return table.concat(t, ',')
