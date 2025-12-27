-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:949
-- @test: CoroutineModuleTUnitTests.CoroutineStatusRemainsAccurateAfterNestedResumes
loggedStatuses = {}

                function outerCoroutine()
                    local inner = coroutine.create(function()
                        coroutine.yield('inner-yield')
                        return 'inner-done'
                    end)

                    local ok, value = coroutine.resume(inner)
                    table.insert(loggedStatuses, coroutine.status(inner))
                    coroutine.yield('outer-yield')
                    table.insert(loggedStatuses, coroutine.status(inner))
                    ok, value = coroutine.resume(inner)
                    table.insert(loggedStatuses, coroutine.status(inner))
                    return value
                end
