-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:113
-- @test: TailCallTUnitTests.TailRecursionDoesNotGrowDebugStack
-- Compatibility notes: Test targets Lua 5.2+
local max_depth = 0

                local function count_stack()
                    local level = 1
                    while debug.getinfo(level, 'n') do
                        level = level + 1
                    end

                    if level > max_depth then
                        max_depth = level
                    end
                end

                local function recur(n)
                    count_stack()
                    if n == 0 then
                        return max_depth
                    end

                    return recur(n - 1)
                end

                return recur(100)
