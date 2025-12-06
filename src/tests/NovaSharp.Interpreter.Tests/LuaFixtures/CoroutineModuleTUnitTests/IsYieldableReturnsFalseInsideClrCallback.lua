-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:600
-- @test: CoroutineModuleTUnitTests.IsYieldableReturnsFalseInsideClrCallback
function invokeClrCheck()
                    return clrCheck()
                end
