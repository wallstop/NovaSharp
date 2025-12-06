-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/TailCallTUnitTests.cs:61
-- @test: TailCallTUnitTests.RecursiveSumMatchesArithmeticBaseline
-- @compat-notes: Lua 5.3+: bitwise operators
local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(10, 0)
