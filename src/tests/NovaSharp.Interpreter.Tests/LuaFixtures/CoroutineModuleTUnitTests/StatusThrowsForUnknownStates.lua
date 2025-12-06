-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:144
-- @test: CoroutineModuleTUnitTests.StatusThrowsForUnknownStates
function idle()
                    return 1
                end
