-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/MetatableTUnitTests.cs:97
-- @test: MetatableTUnitTests.TableValuedCallMetamethodDoesNotChainBeforeLua54
-- Compatibility notes: Test targets Lua 5.3+
local target = {}
                    local proxy = {}
                    setmetatable(target, { __call = proxy })
                    setmetatable(proxy, {
                        __call = function()
                            return 'unexpected'
                        end
                    })
                    return target()
