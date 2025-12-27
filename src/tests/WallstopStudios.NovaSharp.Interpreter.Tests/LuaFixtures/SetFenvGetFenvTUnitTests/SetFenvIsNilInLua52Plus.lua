-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:69
-- @test: SetFenvGetFenvTUnitTests.SetFenvIsNilInLua52Plus
-- @compat-notes: Test targets Lua 5.1
return setfenv
