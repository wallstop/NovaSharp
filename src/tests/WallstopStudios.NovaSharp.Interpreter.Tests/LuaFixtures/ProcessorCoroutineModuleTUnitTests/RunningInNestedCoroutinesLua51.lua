-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:1019
-- @test: ProcessorCoroutineModuleTUnitTests.RunningInNestedCoroutinesLua51
-- Compatibility notes: Test targets Lua 5.1
results = {}
                function outer()
                    results.outer = coroutine.running()
                    
                    local inner = coroutine.create(function()
                        results.inner = coroutine.running()
                    end)
                    
                    coroutine.resume(inner)
                end
