-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:121
-- @test: MetatableTUnitTests.TableValuedCallMetamethodChainsFromLua54
-- Compatibility notes: Test targets Lua 5.4+
local target = {}
                local proxy = {}
                setmetatable(target, { __call = proxy })
                setmetatable(proxy, {
                    __call = function(...)
                        local a, b, c = ...
                        return select('#', ...), a == proxy, b == target, c == nil
                    end
                })
                return target()
