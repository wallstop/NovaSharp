-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: true
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:227
-- @test: Utf8ModuleTUnitTests.Utf8CharpatternMatchesLuaSpecification
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.charpattern
