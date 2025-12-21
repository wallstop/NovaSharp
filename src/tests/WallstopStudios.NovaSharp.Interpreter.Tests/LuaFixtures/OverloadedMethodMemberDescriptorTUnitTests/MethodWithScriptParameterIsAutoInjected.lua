-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:857
-- @test: OverloadedMethodMemberDescriptorTUnitTests.MethodWithScriptParameterIsAutoInjected
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                return obj.GetScriptName('test')
