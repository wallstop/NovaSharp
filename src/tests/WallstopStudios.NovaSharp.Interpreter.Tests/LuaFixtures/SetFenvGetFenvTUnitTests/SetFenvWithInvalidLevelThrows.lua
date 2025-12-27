-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/SetFenvGetFenvTUnitTests.cs:224
-- @test: SetFenvGetFenvTUnitTests.SetFenvWithInvalidLevelThrows
-- @compat-notes: Test targets Lua 5.1
setfenv(100, {})
