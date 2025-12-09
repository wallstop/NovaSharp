-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:60
-- @test: DispatchingUserDataDescriptorTUnitTests.ComparisonOperatorsUseComparable
-- @compat-notes: Lua 5.3+: bitwise operators
return hostAdd == hostCopy
