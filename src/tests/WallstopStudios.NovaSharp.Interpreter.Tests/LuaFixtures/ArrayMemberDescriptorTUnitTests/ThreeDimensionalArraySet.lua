-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:181
-- @test: ArrayMemberDescriptorTUnitTests.ThreeDimensionalArraySet
-- Compatibility notes: Uses injected variable: arr
arr[1, 0, 1] = 77
