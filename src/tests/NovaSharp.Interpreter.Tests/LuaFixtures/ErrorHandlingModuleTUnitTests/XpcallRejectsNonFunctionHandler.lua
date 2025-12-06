-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:315
-- @test: ErrorHandlingModuleTUnitTests.XpcallRejectsNonFunctionHandler
return xpcall(function() end, 123)
