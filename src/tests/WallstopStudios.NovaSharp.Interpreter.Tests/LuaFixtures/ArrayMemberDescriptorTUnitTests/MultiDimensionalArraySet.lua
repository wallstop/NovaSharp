-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:139
-- @test: ArrayMemberDescriptorTUnitTests.MultiDimensionalArraySet
-- @compat-notes: Uses injected variable: arr
arr[0, 1] = 42
