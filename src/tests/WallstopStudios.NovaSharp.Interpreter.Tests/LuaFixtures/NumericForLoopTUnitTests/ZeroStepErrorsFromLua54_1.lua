-- @lua-versions: 5.4+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/NumericForLoopTUnitTests.cs:311
-- @test: NumericForLoopTUnitTests.ZeroStepErrorsFromLua54
-- Compatibility notes: Test targets Lua 5.3+
local n = 0
                          for i = 1, 10, 0 do n = n + 1 end
                          return n
