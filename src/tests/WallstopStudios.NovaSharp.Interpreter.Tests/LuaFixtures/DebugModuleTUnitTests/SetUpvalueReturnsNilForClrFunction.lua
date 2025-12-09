-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:259
-- @test: DebugModuleTUnitTests.SetUpvalueReturnsNilForClrFunction
return debug.setupvalue(print, 1, 'test')
