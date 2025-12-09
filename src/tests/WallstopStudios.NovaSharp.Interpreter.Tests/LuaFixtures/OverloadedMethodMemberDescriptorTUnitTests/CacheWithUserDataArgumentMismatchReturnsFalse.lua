-- @lua-versions: 5.3, 5.4, 5.5
-- @novasharp-only: false
-- @expects-error: true
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:406
-- @test: OverloadedMethodMemberDescriptorTUnitTests.CacheWithUserDataArgumentMismatchReturnsFalse
-- @compat-notes: Lua 5.3+: bitwise operators
local obj = TestClass.__new()
                local arg1 = Arg1.__new()
                return obj.WithUserData(arg1)
