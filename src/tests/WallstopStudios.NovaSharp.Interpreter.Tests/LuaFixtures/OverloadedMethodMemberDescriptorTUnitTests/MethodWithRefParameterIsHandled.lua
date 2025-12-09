-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:631
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodWithRefParameterIsHandled
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                local value = obj.Increment(10)
                return value
