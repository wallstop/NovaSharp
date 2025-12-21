-- @lua-versions: 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:511
-- @test: DebugModuleTUnitTests.GetUserValueReturnsNilForNonUserData
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.getuservalue (5.2+)
return debug.getuservalue('string')
