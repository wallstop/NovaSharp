-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:62
-- @test: ClosureTUnitTests.ClosureOnParamLambda
-- @compat-notes: Lua 5.3+: bitwise OR
local function g (z)
                    return |a| a + z
                end
                return g(3)(2);
