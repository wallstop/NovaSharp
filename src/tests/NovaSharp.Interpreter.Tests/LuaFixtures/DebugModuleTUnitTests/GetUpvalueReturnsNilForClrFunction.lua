-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:179
-- @test: DebugModuleTUnitTests.GetUpvalueReturnsNilForClrFunction
return debug.getupvalue(print, 1)
