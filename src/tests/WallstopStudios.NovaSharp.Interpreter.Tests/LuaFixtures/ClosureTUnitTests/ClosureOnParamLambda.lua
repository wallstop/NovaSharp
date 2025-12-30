-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:62
-- @test: ClosureTUnitTests.ClosureOnParamLambda
-- @compat-notes: NovaSharp: metalua-style lambda syntax
local function g (z)
                    return |a| a + z
                end
                return g(3)(2);
