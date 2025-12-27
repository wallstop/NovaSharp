-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1305
-- @test: DebugModuleTUnitTests.SetUpValueFromClrFunctionReturnsNil
-- @compat-notes: Test targets Lua 5.1
return debug.setupvalue(print, 1, 'test')
