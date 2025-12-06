-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Units/ErrorHandlingModuleTUnitTests.cs:106
-- @test: ErrorHandlingModuleTUnitTests.PcallDecoratesClrScriptRuntimeExceptions
return pcall(clr)
