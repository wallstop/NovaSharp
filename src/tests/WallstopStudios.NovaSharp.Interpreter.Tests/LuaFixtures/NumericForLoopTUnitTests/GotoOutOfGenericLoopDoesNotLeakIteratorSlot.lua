-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:634
-- @test: NumericForLoopTUnitTests.GotoOutOfGenericLoopDoesNotLeakIteratorSlot
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
local n = 0
                  local t = { 1, 2, 3 }
                  ::top::
                  for k, v in pairs(t) do
                      if n < 50000 then
                          n = n + 1
                          goto top
                      end
                  end
                  return n
