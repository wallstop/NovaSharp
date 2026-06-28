-- @lua-versions: 5.3+
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:384
-- @test: Utf8ModuleTUnitTests.Utf8OffsetRequiresCharacterBoundaries
-- Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.offset(word, 1, 3)
