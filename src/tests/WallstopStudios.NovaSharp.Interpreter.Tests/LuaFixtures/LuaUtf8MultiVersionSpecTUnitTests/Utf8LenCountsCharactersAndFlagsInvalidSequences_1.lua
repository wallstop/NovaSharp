-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Spec\LuaUtf8MultiVersionSpecTUnitTests.cs:39
-- @test: LuaUtf8MultiVersionSpecTUnitTests.Utf8LenCountsCharactersAndFlagsInvalidSequences
-- Test targets Lua 5.2+; Lua 5.3+: utf8 library
return utf8.len(invalid)
