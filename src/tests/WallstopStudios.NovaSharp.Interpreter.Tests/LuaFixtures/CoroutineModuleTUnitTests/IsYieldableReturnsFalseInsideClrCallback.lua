-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:816
-- @test: CoroutineModuleTUnitTests.IsYieldableReturnsFalseInsideClrCallback
-- @compat-notes: Test targets Lua 5.3+
function invokeClrCheck()
                    return clrCheck()
                end
