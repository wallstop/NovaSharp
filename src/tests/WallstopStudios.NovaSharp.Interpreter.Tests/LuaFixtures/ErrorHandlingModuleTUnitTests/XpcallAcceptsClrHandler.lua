-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:298
-- @test: ErrorHandlingModuleTUnitTests.XpcallAcceptsClrHandler
local function fail()
                    error('boom')
                end

                return xpcall(fail, clrhandler)
