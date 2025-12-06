-- @lua-versions: 5.3, 5.4
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:471
-- @test: OverloadedMethodMemberDescriptorTUnitTests.Unknown
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                return obj.WithVarArgs(1, 2, 3)
