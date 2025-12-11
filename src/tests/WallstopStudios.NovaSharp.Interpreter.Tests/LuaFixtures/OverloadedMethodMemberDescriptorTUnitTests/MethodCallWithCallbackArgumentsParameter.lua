-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src\tests\WallstopStudios.NovaSharp.Interpreter.Tests.TUnit\Descriptors\OverloadedMethodMemberDescriptorTUnitTests.cs:744
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodCallWithCallbackArgumentsParameter
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                return obj.CountArgs('a', 'b', 'c')
