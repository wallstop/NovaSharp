-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/VM/CoroutineLifecycleTUnitTests.cs:132
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead
-- @compat-notes: Lua 5.3+: bitwise operators
function busy()
                    for i = 1, 200 do end
                    return 'finished'
                end
