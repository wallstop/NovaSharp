-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:220
-- @test: DebugModuleTUnitTests.UpvalueIdReturnsNilForClrFunction
return debug.upvalueid(print, 1)
