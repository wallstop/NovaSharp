-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/TailCallTUnitTests.cs:549
-- @test: TailCallTUnitTests.ToBeClosedTailPositionCallDoesNotSkipCloseHandlers
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local closed = 0
                local mt = {
                    __close = function()
                        closed = closed + 1
                    end
                }

                local function recur(n)
                    local handle <close> = setmetatable({}, mt)
                    if n == 0 then
                        return closed
                    end

                    return recur(n - 1)
                end

                local before = recur(70000)
                return before, closed
