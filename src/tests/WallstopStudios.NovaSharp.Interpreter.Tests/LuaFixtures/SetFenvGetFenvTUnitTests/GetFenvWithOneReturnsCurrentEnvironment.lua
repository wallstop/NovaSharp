-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:108
-- @test: SetFenvGetFenvTUnitTests.GetFenvWithOneReturnsCurrentEnvironment
-- @compat-notes: Test targets Lua 5.1
return getfenv(1) == _G
