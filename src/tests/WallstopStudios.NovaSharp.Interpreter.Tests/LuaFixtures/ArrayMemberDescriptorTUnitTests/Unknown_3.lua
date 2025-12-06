-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/ArrayMemberDescriptorTUnitTests.cs:132
-- @test: ArrayMemberDescriptorTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
arr[0, 1] = 42
