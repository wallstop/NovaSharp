-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:240
-- @test: ClosureTUnitTests.ClosureOnParamLambda
-- Compatibility notes: NovaSharp: metalua-style lambda syntax
local function g (z)
                    return |a| a + z
                end
                return g(3)(2);
