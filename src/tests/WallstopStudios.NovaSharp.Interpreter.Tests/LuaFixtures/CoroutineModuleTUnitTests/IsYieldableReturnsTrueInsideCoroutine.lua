-- @lua-versions: 5.2+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\CoroutineModuleTUnitTests.cs:594
-- @test: CoroutineModuleTUnitTests.IsYieldableReturnsTrueInsideCoroutine
-- Test targets Lua 5.2+
function buildYieldableChecker()
                    return coroutine.wrap(function()
                        return coroutine.isyieldable()
                    end)
                end
