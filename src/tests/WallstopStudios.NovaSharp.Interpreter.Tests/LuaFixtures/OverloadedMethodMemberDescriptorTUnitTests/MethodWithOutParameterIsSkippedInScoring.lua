-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:1042
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodWithOutParameterIsSkippedInScoring
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local value, outVal = obj.TryGetValue('test')
                return value
