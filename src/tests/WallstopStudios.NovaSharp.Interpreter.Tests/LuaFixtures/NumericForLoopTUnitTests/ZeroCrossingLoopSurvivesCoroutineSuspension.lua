-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:379
-- @test: NumericForLoopTUnitTests.ZeroCrossingLoopSurvivesCoroutineSuspension
local co = coroutine.create(function()
                      local t = {}
                      for i = -2, 2 do
                          coroutine.yield(i)
                          t[#t + 1] = i
                      end
                      return table.concat(t, ',')
                  end)
                  local out = {}
                  repeat
                      local ok, v = coroutine.resume(co)
                      out[#out + 1] = tostring(v)
                  until coroutine.status(co) == 'dead'
                  return table.concat(out, ',')
