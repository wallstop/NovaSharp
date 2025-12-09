-- @lua-versions: 5.2, 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:294
-- @test: DebugModuleTUnitTests.GetUserValueReturnsNilForNonUserData
-- @compat-notes: Lua 5.2+: debug.getuservalue (5.2+)
return debug.getuservalue('string')
