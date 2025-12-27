-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:250
-- @test: SetFenvGetFenvTUnitTests.SetFenvOnClrFunctionThrows
-- @compat-notes: Test targets Lua 5.1
setfenv(print, {})
