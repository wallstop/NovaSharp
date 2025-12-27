-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:189
-- @test: ErrorHandlingModuleTUnitTests.PcallRejectsClrTailCallWithContinuation
return pcall(tailing)
