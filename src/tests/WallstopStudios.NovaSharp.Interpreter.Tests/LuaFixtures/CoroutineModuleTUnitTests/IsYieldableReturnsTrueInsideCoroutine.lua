-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:478
-- @test: CoroutineModuleTUnitTests.IsYieldableReturnsTrueInsideCoroutine
function buildYieldableChecker()
                    return coroutine.wrap(function()
                        return coroutine.isyieldable()
                    end)
                end
