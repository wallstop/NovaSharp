-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:1447
-- @test: CoroutineModuleTUnitTests.ExplicitIsYieldableUsesTargetProcessorState
-- Compatibility notes: Test targets Lua 5.4+
function runExplicitCheck()
                    local task = coroutine.create(function()
                        return clrCheckExplicit(coroutine.running())
                    end)
                    local resumed, value = coroutine.resume(task)
                    return resumed, value
                end
