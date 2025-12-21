-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:808
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodWithRefParameterIsHandled
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local value = obj.Increment(10)
                return value
