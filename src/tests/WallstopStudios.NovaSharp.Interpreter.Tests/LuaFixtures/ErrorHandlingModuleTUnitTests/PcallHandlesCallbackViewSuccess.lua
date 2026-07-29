-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Units/CoreLib/ErrorHandlingModuleTUnitTests.cs:232
-- @test: ErrorHandlingModuleTUnitTests.PcallHandlesCallbackViewSuccess
return pcall(clr, 1, 2, 3)
