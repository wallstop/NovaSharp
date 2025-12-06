-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/DispatchingUserDataDescriptorTUnitTests.cs:60
-- @test: DispatchingUserDataDescriptorTUnitTests.ComparisonOperatorsUseComparable
-- @compat-notes: Lua 5.3+: bitwise operators
return hostAdd == hostCopy
