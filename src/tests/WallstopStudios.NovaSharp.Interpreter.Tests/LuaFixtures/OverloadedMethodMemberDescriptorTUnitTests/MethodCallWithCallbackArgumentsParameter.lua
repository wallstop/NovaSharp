-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:1212
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodCallWithCallbackArgumentsParameter
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.CountArgs('a', 'b', 'c')
