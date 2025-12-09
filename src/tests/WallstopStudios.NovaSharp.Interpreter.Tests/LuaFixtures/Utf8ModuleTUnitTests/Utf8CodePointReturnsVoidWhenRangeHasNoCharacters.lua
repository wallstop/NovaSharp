-- @lua-versions: 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Modules\Utf8ModuleTUnitTests.cs:140
-- @test: Utf8ModuleTUnitTests.Utf8CodePointReturnsVoidWhenRangeHasNoCharacters
-- @compat-notes: Test targets Lua 5.4+; Lua 5.3+: utf8 library
return utf8.codepoint('abc', 5, 4)
