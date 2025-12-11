-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:300
-- @test: Utf8ModuleTUnitTests.Utf8OffsetThrowsForPositionZero
-- @compat-notes: utf8.offset throws "position out of bounds" for position 0
return utf8.offset('abcd', 1, 0)
