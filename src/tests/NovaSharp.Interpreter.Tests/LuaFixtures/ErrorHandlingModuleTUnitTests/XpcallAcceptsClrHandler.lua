-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:279
-- @test: ErrorHandlingModuleTUnitTests.XpcallAcceptsClrHandler
local function fail()
                    error('boom')
                end

                return xpcall(fail, clrhandler)
