-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:268
-- @test: SetFenvGetFenvTUnitTests.GetFenvOnClrFunctionReturnsGlobal
-- @compat-notes: Test targets Lua 5.1
return getfenv(print) == _G
