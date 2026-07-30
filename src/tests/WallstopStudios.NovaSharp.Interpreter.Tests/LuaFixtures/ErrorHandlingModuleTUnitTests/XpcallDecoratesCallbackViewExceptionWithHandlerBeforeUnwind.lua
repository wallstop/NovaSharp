-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:535
-- @test: ErrorHandlingModuleTUnitTests.XpcallDecoratesCallbackViewExceptionWithHandlerBeforeUnwind
function decorator(message)
                    return 'decorated:' .. message
                end
