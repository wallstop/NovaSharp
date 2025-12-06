-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:183
-- @test: OverloadedMethodMemberDescriptorTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                return obj.WithInt(42)
