-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/IoModuleTUnitTests.cs:744
-- @test: IoModuleTUnitTests.TypeReturnsNilForNonUserData
-- @compat-notes: Test targets Lua 5.1
return io.type(123)
