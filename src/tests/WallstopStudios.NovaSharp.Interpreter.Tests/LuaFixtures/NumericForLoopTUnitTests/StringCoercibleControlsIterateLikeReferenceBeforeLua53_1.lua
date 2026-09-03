-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:647
-- @test: NumericForLoopTUnitTests.StringCoercibleControlsIterateLikeReferenceBeforeLua53
-- Compatibility notes: Test targets Lua 5.2+
local t = {}
                  for i = 1, 3, '1' do t[#t + 1] = tostring(i) end
                  return table.concat(t, ',')
