-- @lua-versions: novasharp-only
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:132
-- @test: ArrayMemberDescriptorTUnitTests.MultiDimensionalArraySet
-- @compat-notes: Lua 5.3+: bitwise operators; Uses injected variable: arr
arr[0, 1] = 42
