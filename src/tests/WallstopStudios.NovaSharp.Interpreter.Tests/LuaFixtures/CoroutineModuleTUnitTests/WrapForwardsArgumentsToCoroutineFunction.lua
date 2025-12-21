-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:592
-- @test: CoroutineModuleTUnitTests.WrapForwardsArgumentsToCoroutineFunction
-- @compat-notes: Test targets Lua 5.1
function buildConcatWrapper()
                    return coroutine.wrap(function(a, b, c)
                        return table.concat({a, b, c}, '-')
                    end)
                end
