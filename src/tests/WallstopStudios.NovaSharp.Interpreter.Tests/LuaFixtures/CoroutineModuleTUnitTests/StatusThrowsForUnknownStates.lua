-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/CoroutineModuleTUnitTests.cs:176
-- @test: CoroutineModuleTUnitTests.StatusThrowsForUnknownStates
function idle()
                    return 1
                end
