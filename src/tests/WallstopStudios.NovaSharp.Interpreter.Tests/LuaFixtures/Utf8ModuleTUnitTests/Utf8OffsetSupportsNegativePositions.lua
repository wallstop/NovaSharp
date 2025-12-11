-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Modules/Utf8ModuleTUnitTests.cs:288
-- @test: Utf8ModuleTUnitTests.Utf8OffsetSupportsNegativePositions
-- @compat-notes: utf8.offset supports negative positions (counting from end)
return utf8.offset('abcd', 1, -1)
