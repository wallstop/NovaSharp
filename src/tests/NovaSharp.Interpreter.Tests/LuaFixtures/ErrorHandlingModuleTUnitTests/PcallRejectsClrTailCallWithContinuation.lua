-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:177
-- @test: ErrorHandlingModuleTUnitTests.PcallRejectsClrTailCallWithContinuation
return pcall(tailing)
