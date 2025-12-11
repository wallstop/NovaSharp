-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Sandbox\SandboxMemoryLimitTUnitTests.cs:830
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerCombinedTableClosureCoroutine
-- @compat-notes: Test class 'SandboxMemoryLimitTUnitTests' uses NovaSharp-specific Sandbox functionality
-- Tables
                local t1 = { a = 1, b = 2 }
                local t2 = { x = 10, y = 20, z = 30 }
                
                -- Closures with upvalues
                local counter = 0
                function increment()
                    counter = counter + 1
                    return counter
                end
                
                -- Coroutine
                function producer()
                    for i = 1, 10 do
                        coroutine.yield(i)
                    end
                end
                co = coroutine.create(producer)
