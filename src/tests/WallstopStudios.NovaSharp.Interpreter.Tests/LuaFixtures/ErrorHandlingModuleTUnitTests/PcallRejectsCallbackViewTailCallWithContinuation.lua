-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:449
-- @test: ErrorHandlingModuleTUnitTests.PcallRejectsCallbackViewTailCallWithContinuation
return pcall(tailing)
