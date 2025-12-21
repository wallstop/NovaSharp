-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:61
-- @test: SetFenvGetFenvTUnitTests.GetFenvIsNilInLua52Plus
-- @compat-notes: Test targets Lua 5.1
return getfenv
