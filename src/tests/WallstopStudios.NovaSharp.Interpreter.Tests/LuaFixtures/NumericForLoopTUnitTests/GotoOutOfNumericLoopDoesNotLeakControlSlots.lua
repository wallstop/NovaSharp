-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:612
-- @test: NumericForLoopTUnitTests.GotoOutOfNumericLoopDoesNotLeakControlSlots
-- Compatibility notes: Test targets Lua 5.2+; Lua 5.2+: goto statement (5.2+); Lua 5.2+: label (5.2+)
local n = 0
                  ::top::
                  for i = 1, 2 do
                      if n < 100000 then
                          n = n + 1
                          goto top
                      end
                  end
                  return n
