-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:150
-- @test: Utf8ModuleTUnitTests.Utf8CharpatternMatchesLuaSpecification
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: utf8 library
return utf8.charpattern
