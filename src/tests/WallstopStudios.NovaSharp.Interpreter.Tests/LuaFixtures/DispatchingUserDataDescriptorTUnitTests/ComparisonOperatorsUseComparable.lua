-- @lua-versions: 5.1+
-- @novasharp-only: true
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:64
-- @test: DispatchingUserDataDescriptorTUnitTests.ComparisonOperatorsUseComparable
return hostAdd == hostCopy
