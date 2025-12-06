-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:51
-- @test: ErrorHandlingModuleTUnitTests.PcallRejectsNonFunctions
return pcall(123)
