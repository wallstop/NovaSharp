-- @lua-versions: none
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/DebugModuleTUnitTests.cs:526
-- @test: DebugModuleTUnitTests.GetUserValueReturnsNilFalseForNonUserData54Plus
-- @compat-notes: Test targets Lua 5.1; Lua 5.2+: debug.getuservalue (5.2+)
return debug.getuservalue('string')
