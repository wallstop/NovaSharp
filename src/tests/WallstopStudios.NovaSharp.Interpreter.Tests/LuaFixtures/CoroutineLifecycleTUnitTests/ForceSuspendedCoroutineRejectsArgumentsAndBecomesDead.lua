-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/DataTypes/CoroutineLifecycleTUnitTests.cs:168
-- @test: CoroutineLifecycleTUnitTests.ForceSuspendedCoroutineRejectsArgumentsAndBecomesDead
-- Test targets Lua 5.1
function busy()
                    for i = 1, 200 do end
                    return 'finished'
                end
