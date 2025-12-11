-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Units\DataTypes\TailCallTUnitTests.cs:82
-- @test: TailCallTUnitTests.RecursiveSumHandlesVeryDeepTailRecursion
-- @compat-notes: Lua 5.3+: bitwise operators
local function recsum(n, partial)
                    if n == 0 then
                        return partial
                    end
                    return recsum(n - 1, partial + n)
                end

                return recsum(70000, 0)
