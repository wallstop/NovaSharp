-- @lua-versions: 5.1, 5.5
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:703
-- @test: OverloadedMethodMemberDescriptorTUnitTests.CacheMismatchWhenCachedUserDataButCallWithNonUserData
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local arg = Arg1.__new()
                return obj.MixedArgs(arg)
