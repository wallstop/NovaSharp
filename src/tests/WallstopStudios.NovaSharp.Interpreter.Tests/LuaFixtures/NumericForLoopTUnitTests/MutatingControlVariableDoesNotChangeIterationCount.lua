-- @lua-versions: 5.1-5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:352
-- @test: NumericForLoopTUnitTests.MutatingControlVariableDoesNotChangeIterationCount
-- Compatibility notes: Test targets Lua 5.4+
local t = {}
                  for i = -2, 2 do t[#t + 1] = i i = i + 100 end
                  return table.concat(t, ',')
