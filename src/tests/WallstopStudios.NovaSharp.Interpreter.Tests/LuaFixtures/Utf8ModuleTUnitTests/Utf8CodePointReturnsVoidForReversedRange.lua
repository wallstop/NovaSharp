-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:197
-- @test: Utf8ModuleTUnitTests.Utf8CodePointReturnsVoidForReversedRange
-- @compat-notes: Test targets Lua 5.3+; Lua 5.3+: utf8 library
return utf8.codepoint('abc', 3, 1)
