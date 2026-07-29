-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:149
-- @test: MetatableTUnitTests.TableValuedCallMetamethodChainsInLatestDefault
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
