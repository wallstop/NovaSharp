-- @lua-versions: 5.1
-- @novasharp-only: false
-- @expects-error: false
-- @source: src/tests/WallstopStudios.NovaSharp.Interpreter.Tests.TUnit/Descriptors/OverloadedMethodMemberDescriptorTUnitTests.cs:910
-- @test: OverloadedMethodMemberDescriptorTUnitTests.LastCallCacheWithZeroArguments
-- @compat-notes: Test targets Lua 5.1
local obj = TestClass.__new()
                local total = 0
                for i = 1, 10 do
                    obj.NoArgs()
                    total = total + 1
                end
                return total
