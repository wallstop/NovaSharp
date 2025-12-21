-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1045
-- @test: CoroutineModuleTUnitTests.CoroutineStatusRemainsAccurateAfterNestedResumes
-- @compat-notes: Test targets Lua 5.1
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
