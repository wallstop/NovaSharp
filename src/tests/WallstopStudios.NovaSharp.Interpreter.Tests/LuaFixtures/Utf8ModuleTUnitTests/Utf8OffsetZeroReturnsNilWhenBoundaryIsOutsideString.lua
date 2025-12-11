-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:426
-- @test: Utf8ModuleTUnitTests.Utf8OffsetThrowsForPositionOutOfBounds
-- @compat-notes: utf8.offset throws "position out of bounds" for positions outside valid range [1, length+1]
return utf8.offset('abc', 0, 10)
