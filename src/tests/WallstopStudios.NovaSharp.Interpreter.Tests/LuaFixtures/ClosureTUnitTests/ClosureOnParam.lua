-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/EndToEnd/ClosureTUnitTests.cs:29
-- @test: ClosureTUnitTests.ClosureOnParam
local function g (z)
                    local function f(a)
                        return a + z;
                    end
                    return f;
                end
                return g(3)(2);
