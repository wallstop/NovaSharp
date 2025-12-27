-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:91
-- @test: SetFenvGetFenvTUnitTests.GetFenvWithZeroReturnsGlobalEnvironment
-- @compat-notes: Test targets Lua 5.1
return getfenv(0) == _G
