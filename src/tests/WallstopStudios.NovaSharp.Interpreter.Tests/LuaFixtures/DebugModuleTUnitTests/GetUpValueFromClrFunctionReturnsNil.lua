-- @lua-versions: 5.1+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:703
-- @test: DebugModuleTUnitTests.GetUpValueFromClrFunctionReturnsNil
return debug.getupvalue(print, 1)
