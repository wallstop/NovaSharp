-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:47
-- @test: SetFenvGetFenvTUnitTests.SetFenvExistsInLua51
-- @compat-notes: Test targets Lua 5.1
return type(setfenv)
