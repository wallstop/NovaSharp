-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:537
-- @test: OverloadedMethodMemberDescriptorTUnitTests.CacheWithUserDataArgumentMismatchReturnsFalse
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local arg2 = Arg2.__new()
                return obj.WithUserData(arg2)
