-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:594
-- @test: CoroutineModuleTUnitTests.IsYieldableReturnsTrueInsideCoroutine
-- @compat-notes: Test targets Lua 5.2+
function buildYieldableChecker()
                    return coroutine.wrap(function()
                        return coroutine.isyieldable()
                    end)
                end
