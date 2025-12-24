-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineApiTUnitTests.cs:627
-- @test: ProcessorCoroutineApiTUnitTests.GetStackTraceUsesSuspendedLocationWhenNotRunning
-- @compat-notes: Test targets Lua 5.1
return function()
                    local function inner()
                        coroutine.yield('pause')
                    end

                    inner()
                end
