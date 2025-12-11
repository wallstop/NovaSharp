-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:148
-- @test: Utf8ModuleTUnitTests.Utf8CodePointThrowsForOutOfBoundsRange
-- @compat-notes: utf8.codepoint throws "out of bounds" for positions outside [1, length]
return utf8.codepoint('abc', 5, 4)
