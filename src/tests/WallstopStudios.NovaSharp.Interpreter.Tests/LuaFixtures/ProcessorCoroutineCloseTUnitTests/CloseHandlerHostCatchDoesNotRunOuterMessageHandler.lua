-- @lua-versions: 5.4, 5.5
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineCloseTUnitTests.cs:2134
-- @test: ProcessorCoroutineCloseTUnitTests.CloseHandlerHostCatchDoesNotRunOuterMessageHandler
-- Compatibility notes: Lua 5.4+: close attribute
local resource = setmetatable({}, {
                    __close = function() worker() end
                })
                local ok, err = xpcall(function()
                    local pending <close> = resource
                end, messageHandler)

                return ok, tostring(err)
