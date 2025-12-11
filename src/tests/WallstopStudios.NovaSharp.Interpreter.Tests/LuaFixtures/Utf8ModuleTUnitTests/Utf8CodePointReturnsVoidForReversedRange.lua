-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:136
-- @test: Utf8ModuleTUnitTests.Utf8CodePointReturnsVoidForReversedRange
-- @compat-notes: utf8.codepoint returns no values when start > end (reversed range)
return utf8.codepoint('abc', 3, 1)
