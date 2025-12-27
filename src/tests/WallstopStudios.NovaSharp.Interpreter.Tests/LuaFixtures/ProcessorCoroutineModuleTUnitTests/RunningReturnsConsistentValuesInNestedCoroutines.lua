-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/Execution/ProcessorExecution/ProcessorCoroutineModuleTUnitTests.cs:577
-- @test: ProcessorCoroutineModuleTUnitTests.RunningReturnsConsistentValuesInNestedCoroutines
-- @compat-notes: Test targets Lua 5.2+
results = {}
                function outer()
                    local co, isMain = coroutine.running()
                    results.outer = { co = co, isMain = isMain }
                    
                    local inner = coroutine.create(function()
                        local co2, isMain2 = coroutine.running()
                        results.inner = { co = co2, isMain = isMain2 }
                    end)
                    
                    coroutine.resume(inner)
                    return results
                end
