-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Sandbox/SandboxMemoryLimitTUnitTests.cs:830
-- @test: SandboxMemoryLimitTUnitTests.ScriptAllocationTrackerCombinedTableClosureCoroutine
-- @compat-notes: Lua 5.3+: bitwise operators
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
