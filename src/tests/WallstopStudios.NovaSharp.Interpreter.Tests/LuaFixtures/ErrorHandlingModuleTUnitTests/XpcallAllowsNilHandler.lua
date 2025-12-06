-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:299
-- @test: ErrorHandlingModuleTUnitTests.XpcallAllowsNilHandler
return xpcall(function() error('nil-handler') end, nil)
