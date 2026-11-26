-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1230
-- @test: CoroutineModuleTUnitTests.WrapPreservesRejectedResumeInsideCloseHandler
-- Compatibility notes: Test targets Lua 5.4+; Lua 5.4+: close attribute
local wrapper
                local observed
                wrapper = coroutine.wrap(function()
                    local pending <close> = setmetatable({}, {
                        __close = function()
                            local ok, err = pcall(wrapper)
                            observed = tostring(ok) .. ':' .. tostring(err) .. ':' .. type(err)
                        end
                    })
                    coroutine.yield('pause')
                    error('boom', 0)
                end)

                local first = wrapper()
                local ok, err = pcall(wrapper)

                print('first', first)
                print('second', ok, err)
                print('observed', observed)

                return first, ok, err, observed
