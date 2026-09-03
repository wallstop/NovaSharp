-- @lua-versions: 5.1-5.2
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:520
-- @test: NumericForLoopTUnitTests.FloatLoopControlsKeepIntegralFormattingBeforeLua53
-- Compatibility notes: Test targets Lua 5.2+
local t = {}
                  for i = 1, 3.0 do t[#t + 1] = tostring(i) end
                  return table.concat(t, ',')
