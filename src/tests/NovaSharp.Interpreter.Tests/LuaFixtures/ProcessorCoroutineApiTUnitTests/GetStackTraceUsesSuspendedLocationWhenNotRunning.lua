-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/ProcessorCoroutineApiTUnitTests.cs:450
-- @test: ProcessorCoroutineApiTUnitTests.GetStackTraceUsesSuspendedLocationWhenNotRunning
return function()
                    local function inner()
                        coroutine.yield('pause')
                    end

                    inner()
                end
