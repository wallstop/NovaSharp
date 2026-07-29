-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:123
-- @test: ArrayMemberDescriptorTUnitTests.MultiDimensionalArrayAccess
-- Compatibility notes: Uses injected variable: arr
return arr[1, 1]
