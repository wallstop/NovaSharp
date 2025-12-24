-- @lua-versions: 5.1, 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:1080
-- @test: DebugModuleTUnitTests.SetUpValueFromClrFunctionReturnsNil
-- @compat-notes: Test targets Lua 5.1
return debug.setupvalue(print, 1, 'test')
